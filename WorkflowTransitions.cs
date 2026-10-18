namespace MusicLabel;

public abstract record WorkflowState
{
    private WorkflowState() {}

    public record Pending(RawReleaseRecord Raw) : WorkflowState;

    public record Validated(ValidRelease Release) : WorkflowState;

    public record Processed(ValidRelease Release, ReleaseQuote Quote) : WorkflowState;

    public record Failed(string Reason) : WorkflowState;
}

public static class WorkflowTransitions
{
    public static WorkflowState ValidateStep(WorkflowState state) => state switch
    {
        WorkflowState.Pending(var raw) => ReleaseValidator.ParseAndValidate(raw) switch
        {
            ReleaseResult.Valid(var validRelease) => new WorkflowState.Validated(validRelease),
            ReleaseResult.Invalid(var reason) => new WorkflowState.Failed(reason),
            _ => new WorkflowState.Failed("Invalid state transition")
        },
        WorkflowState.Validated or WorkflowState.Processed or WorkflowState.Failed => state,
        _ => new WorkflowState.Failed("Invalid state transition")
    };

    public static WorkflowState ProcessStep(WorkflowState state) => state switch
        {
            WorkflowState.Validated(var release) =>
                new WorkflowState.Processed(release, PipelineStages.ExecutePricing(release,PricingRules.ForTier(release.Tier))),
            WorkflowState.Processed or WorkflowState.Failed => state,
            
            WorkflowState.Pending _ =>
                new WorkflowState.Failed("Prerequisite not met: Release must be validated before processing."),
            _ => new WorkflowState.Failed("Invalid state transition")
        };
}