using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Model;

public sealed record NormalizedSearchQuery
{
    private NormalizedSearchQuery(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static NormalizedSearchQuery FromText(string value)
    {
        return new NormalizedSearchQuery(MusicIdentityText.NormalizeFreeText(value));
    }

    public CatalogSearchAttempt ToNewCatalogSearchAttempt(CatalogSearchCriteria criteria)
    {
        return new CatalogSearchAttempt(
            criteria,
            this.Value,
            TrustLevel: 0,
            RiskScore: 0,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: CorrelationId.New());
    }
    
    public static implicit operator string(NormalizedSearchQuery query) => query.Value;
    
    public static implicit operator NormalizedSearchQuery(string? query) => new(query ?? throw new ArgumentNullException(nameof(query)));
}
