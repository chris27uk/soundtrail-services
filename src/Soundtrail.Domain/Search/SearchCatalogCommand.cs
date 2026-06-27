using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Search;

public sealed record SearchCatalogCommand(
    string Query,
    SearchTypesFilter Types,
    PlaybackProviderFilter Playback,
    SearchLimit Limit,
    SearchOffset Offset)
{
    public MusicSearchCriteria ToMusicSearchTerm() => MusicSearchCriteria.ByQuery(Query, Types);

    public CatalogSearchRequested ToCatalogSearchAttempt() =>
        new(
            MusicSeekOrSearchCriteria.FromSearch(ToMusicSearchTerm()),
            Playback,
            TrustLevel: 0,
            RiskScore: 0,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: CorrelationId.New());
}
