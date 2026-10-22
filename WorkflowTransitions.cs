namespace MusicLabel;

public abstract record WorkflowState
{
    private WorkflowState() {}

    public record Pending(RawReleaseRecord Raw) : WorkflowState;

    public record Validated(ValidRelease Release) : WorkflowState;

    public record Processed(ValidRelease Release, ReleaseQuote Quote) : WorkflowState;

    public record Failed(Error Error) : WorkflowState
    {
        public string Reason => Error.Message;
    };
}

public static class WorkflowTransitions
{
    public static WorkflowState ValidateStep(WorkflowState state) => state switch
    {
        WorkflowState.Pending(var raw) => ReleaseValidator.ParseAndValidate(raw) switch
        {
            Result<ValidRelease>.Success(var validRelease) => new WorkflowState.Validated(validRelease),
            Result<ValidRelease>.Failure(var error) => new WorkflowState.Failed(error),
            _ => new WorkflowState.Failed(Errors.InvalidTransition("Pending"))
        },
        WorkflowState.Validated or WorkflowState.Processed or WorkflowState.Failed => state,
        _ => new WorkflowState.Failed(Errors.InvalidTransition("Unknown"))
    };

    public static Result<ValidRelease> RequireValidated(WorkflowState state) => state switch
    {
        WorkflowState.Validated(var validRelease) => Result<ValidRelease>.Ok(validRelease),
        WorkflowState.Pending => Result<ValidRelease>.Fail(Errors.PrerequisiteNotMet()),
        WorkflowState.Processed => Result<ValidRelease>.Fail(Errors.InvalidTransition("Processed")),
        WorkflowState.Failed(var error) => Result<ValidRelease>.Fail(error),
        _ => Result<ValidRelease>.Fail(Errors.InvalidTransition("Unknown"))
    };

    public static WorkflowState ProcessStep(WorkflowState state) => state switch
        {
            WorkflowState.Processed or WorkflowState.Failed => state,
            
            _ => RequireValidated(state) switch
            {
                Result<ValidRelease>.Success(var release) => new WorkflowState.Processed(release,
                    PipelineStages.ExecutePricing(release, PricingRules.ForTier(release.Tier))),
                Result<ValidRelease>.Failure(var error) => new WorkflowState.Failed(error),
                _ => new WorkflowState.Failed(Errors.InvalidTransition("Unknown"))
            }
        };
}