using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Enrichment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupHistoryChanged.Support;

public sealed record MusicCatalogLookupHistoryChangedCommand(
    MusicCatalogLookupId LookupId,
    IReadOnlyList<(int Version, IDomainEvent Event)> Events);
