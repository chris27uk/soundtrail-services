using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public sealed class LookupMusicRequestListener(LookupMusicRequestHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LookupMusicRequest request,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.ScheduleAsync(request, cancellationToken);
        return result.ShouldSchedule
            ? [result.Command!.ToResolveCanonicalMetadataCommand()]
            : [];
    }
}
