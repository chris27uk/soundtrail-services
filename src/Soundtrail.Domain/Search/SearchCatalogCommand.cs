using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Search;

public sealed record SearchCatalogCommand(
    NormalizedSearchQuery Query,
    SearchTypesFilter Types,
    PlaybackProviderFilter Playback,
    SearchLimit Limit,
    SearchOffset Offset)
{
    public CatalogSearchCriteria ToCatalogSearchCriteria() => CatalogSearchCriteria.Search(Types.ToCriteriaType(), Query.Value);
}
