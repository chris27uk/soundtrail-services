using Soundtrail.Contracts;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

public sealed record NormalizedSearchQuery
{
    private NormalizedSearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NormalizedSearchQuery FromText(string value)
    {
        var sanitized = new string(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
                .ToArray());

        var normalized = string.Join(
            ' ',
            sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return new NormalizedSearchQuery(normalized);
    }

    public LookupMusicRequest ToNewLookupRequest()
    {
        return new LookupMusicRequest(
            this.Value,
            TrustLevel: 0,
            RiskScore: 0,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: CorrelationId.New());
    }
    
    public static implicit operator string(NormalizedSearchQuery query) => query.Value;
    
    public static implicit operator NormalizedSearchQuery(string query) => new(query);
}
