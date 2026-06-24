namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Configuration;

public sealed class SourceApiBudgetsOptions
{
    public const string SectionName = "SourceBudgets";

    public SourceApiBudgetPolicyOptions MusicBrainz { get; init; } = new()
    {
        MaxRequests = 60,
        WindowSeconds = 60,
        SafetyMarginPercent = 10,
        MinimumSpacingSeconds = 1
    };

    public SourceApiBudgetPolicyOptions Odesli { get; init; } = new()
    {
        MaxRequests = 300,
        WindowSeconds = 60,
        SafetyMarginPercent = 10,
        MinimumSpacingSeconds = null
    };
}
