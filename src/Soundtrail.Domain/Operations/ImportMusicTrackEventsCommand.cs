using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Operations;

public sealed record ImportMusicTrackEventsCommand(
    CatalogItemId MusicCatalogId,
    int ExpectedVersion,
    CommandId CommandId,
    IReadOnlyList<IMusicTrackEvent> Events);
