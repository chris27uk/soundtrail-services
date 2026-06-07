using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Worker;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.MusicBrainzLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class MusicBrainzLookupExecutionListener(ExecuteMusicBrainzLookupHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolveCanonicalMetadataCommand message,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(message, cancellationToken);
        return result.Response is null ? [] : [result.Response];
    }
}
