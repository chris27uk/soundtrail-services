using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class EnrichmentResponseListener(ApplyEnrichmentResponseHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        EnrichmentResponse response,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        (await handler.Handle(response, cancellationToken))
        .Facts
        .Where(fact => fact is AppleMusicResolutionRequired or YouTubeMusicResolutionRequired)
        .Cast<object>()
        .ToArray();
}
