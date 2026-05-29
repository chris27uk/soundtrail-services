namespace Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

public sealed record ProviderBudgetDecision(
    bool Allowed,
    int MinuteUsed,
    int MinuteLimit,
    int HourUsed,
    int HourLimit,
    int DayUsed,
    int DayLimit);
