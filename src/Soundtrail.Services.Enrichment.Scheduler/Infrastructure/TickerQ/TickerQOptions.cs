namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.TickerQ;

public sealed class TickerQOptions
{
    public const string SectionName = "TickerQ";

    public string ConnectionString { get; init; } = "Data Source=soundtrail-scheduler.db";

    public TickerQDashboardOptions Dashboard { get; init; } = new();
}

public sealed class TickerQDashboardOptions
{
    public string BasePath { get; init; } = "/tickerq";

    public string? Username { get; init; }

    public string? Password { get; init; }
}
