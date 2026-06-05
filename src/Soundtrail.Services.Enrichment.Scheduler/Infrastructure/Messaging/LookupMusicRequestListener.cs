using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Features.Search.Queueing;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

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
            ? [result.Command!.ToMusicBrainzTransportMessage()]
            : [];
    }
}
