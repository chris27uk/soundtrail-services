using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters.Indexes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven;

public sealed class RavenIndexesHostedService(IDocumentStore documentStore) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await IndexCreation.CreateIndexesAsync(typeof(TrackCatalogue_BySearchText).Assembly, documentStore, token: cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
