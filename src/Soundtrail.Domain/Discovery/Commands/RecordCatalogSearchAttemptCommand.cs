using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record RecordCatalogSearchAttemptCommand(
    MusicSearchCriteria SearchCriteria,
    SearchCatalogRequested Requested);
