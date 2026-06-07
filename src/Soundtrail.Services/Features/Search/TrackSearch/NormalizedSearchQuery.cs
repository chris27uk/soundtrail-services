using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Features.Search.TrackSearch;

public sealed record NormalizedSearchQuery
{
    private NormalizedSearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NormalizedSearchQuery From(SearchQuery query)
    {
        return FromText(query.Value);
    }

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
            this,
            TrustLevel: 0,
            RiskScore: 0,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: CorrelationId.New());
    }
}
