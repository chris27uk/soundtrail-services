using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class MusicCatalogLookupAttemptedHandler(
    IEventStreamRepository<MusicCatalogLookupId, IDomainEvent> repository) : IHandler<MusicCatalogLookupAttempted>
{
    public async Task Handle(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken = default)
    {
        var loaded = await MusicCatalogLookupHistory.LoadAsync(
            repository,
            attempted.MusicCatalogId,
            cancellationToken);

        if (!loaded.Aggregate.Record(attempted))
        {
            return;
        }

        await loaded.Aggregate.SaveAsync(repository, loaded.Stream, attempted.CommandId, cancellationToken);
    }
}
