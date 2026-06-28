using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

public sealed record CatalogCandidateIdentifiedCommand(
    MusicSearchCriteria SearchCriteria,
    IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> Events);
