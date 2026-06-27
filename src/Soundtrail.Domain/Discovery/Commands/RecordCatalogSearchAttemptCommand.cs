using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Commands;

public sealed record RecordCatalogSearchAttemptCommand(
    MusicSearchCriteria SearchCriteria,
    SearchCatalogRequested Requested);
