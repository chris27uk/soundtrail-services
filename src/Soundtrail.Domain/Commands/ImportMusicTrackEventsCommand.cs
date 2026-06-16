using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Commands;

public sealed record ImportMusicTrackEventsCommand(
    MusicCatalogId MusicCatalogId,
    int ExpectedVersion,
    CommandId CommandId,
    IReadOnlyList<IMusicTrackEvent> Events);
