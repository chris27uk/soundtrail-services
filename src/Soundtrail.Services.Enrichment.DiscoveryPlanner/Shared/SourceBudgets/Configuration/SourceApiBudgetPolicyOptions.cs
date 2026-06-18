namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;

public sealed class SourceApiBudgetPolicyOptions
{
    public int MaxRequests { get; init; }

    public int WindowSeconds { get; init; }

    public int SafetyMarginPercent { get; init; }

    public int? MinimumSpacingSeconds { get; init; }
}
