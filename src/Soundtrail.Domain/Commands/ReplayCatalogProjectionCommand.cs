using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Commands;

public sealed record ReplayCatalogProjectionCommand(
    bool ReplayAll,
    IReadOnlyList<MusicCatalogId> MusicCatalogIds);
