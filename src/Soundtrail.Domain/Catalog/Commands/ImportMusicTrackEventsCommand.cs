using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Catalog.Commands;

public sealed record ImportMusicTrackEventsCommand(
    MusicCatalogId MusicCatalogId,
    int ExpectedVersion,
    CommandId CommandId,
    IReadOnlyList<IMusicTrackEvent> Events);
