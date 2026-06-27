using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record ReplayCatalogSearchStatusCommand(MusicSearchCriteria SearchCriteria);
