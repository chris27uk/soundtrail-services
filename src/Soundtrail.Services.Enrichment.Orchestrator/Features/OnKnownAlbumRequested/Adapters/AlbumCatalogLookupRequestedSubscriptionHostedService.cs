using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;

public sealed class AlbumCatalogLookupRequestedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ITypeRegistry registry,
    ILogger<AlbumCatalogLookupRequestedSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "album-catalog-lookup-requested-dispatch";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenStoredEventRecord>(options);
                await worker.Run(batch => ProcessBatchAsync(batch, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Album catalog lookup requested subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<RavenStoredEventRecord>(
                new SubscriptionCreationOptions { Name = SubscriptionName },
                token: cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    private async Task ProcessBatchAsync(
        SubscriptionBatch<RavenStoredEventRecord> batch,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<DispatchKnownAlbumLookupCommandHandler>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.StreamId, StringComparer.Ordinal))
        {
            var events = stream
                .OrderBy(item => item.Version)
                .Select(item => new VersionedCatalogSearchDiscoveryEvent(
                    item.Version,
                    registry.ToDomainObject<IDomainEvent>(
                        item.Body ?? throw new InvalidOperationException($"Stored event '{item.Id}' is missing a body."))))
                .Where(item => item.Event is AlbumCatalogLookupRequested)
                .ToArray();

            if (events.Length == 0)
            {
                continue;
            }

            await handler.Handle(
                new AlbumCatalogLookupRequestedCommand(stream.Key, events),
                cancellationToken);
        }
    }
}
