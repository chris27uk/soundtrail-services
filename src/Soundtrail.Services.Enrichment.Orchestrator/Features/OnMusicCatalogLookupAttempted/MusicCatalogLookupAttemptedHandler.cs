using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted;

public sealed class MusicCatalogLookupAttemptedHandler(
    ICommandBus commandBus) : IHandler<MusicCatalogLookupAttempted>
{
    public async Task Handle(
        MusicCatalogLookupAttempted attempted,
        CancellationToken cancellationToken = default)
    {
        await commandBus.SendAsync(
            new ApplyMusicCatalogLookupAttemptedToCatalogCommand(attempted),
            cancellationToken);
        await commandBus.SendAsync(
            new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(attempted),
            cancellationToken);
    }
}
