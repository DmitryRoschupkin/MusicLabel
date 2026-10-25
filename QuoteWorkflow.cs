namespace MusicLabel;

public static class QuoteWorkflow
{
    public static Result<string> Quote(ReleaseCatalog catalog, string id) =>
        catalog.Find(id)
            .ToResult(() => Errors.ReleaseNotFound(id))
            .Bind(ReleaseValidator.ParseAndValidate)
            .Bind(release => Price(new WorkflowState.Validated(release)))
            .Map(LoyaltyProgram.Apply)
            .Map(Render);

    public static Result<ReleaseQuote> Price(WorkflowState state) =>
        WorkflowTransitions.RequireValidated(state)
            .Map(release => PipelineStages.ExecutePricing(release, PricingRules.ForTier(release.Tier)));

    public static string Render(ReleaseQuote q) =>
        FormattableString.Invariant($"{q.ArtistName} - {q.ReleaseTitle} : {q.TotalCost:F2} [{q.AppliedRule}]");
}