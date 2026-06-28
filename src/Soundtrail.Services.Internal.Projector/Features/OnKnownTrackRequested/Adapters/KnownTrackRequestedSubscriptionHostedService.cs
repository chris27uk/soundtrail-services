using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Support;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Adapters;

public sealed class KnownTrackRequestedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ILogger<KnownTrackRequestedSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "known-track-requested-follow-up";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<DiscoveryQueryStoredEventRecordDto>(options);
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
                logger.LogError(ex, "Known track requested subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<DiscoveryQueryStoredEventRecordDto>(
                new SubscriptionCreationOptions { Name = SubscriptionName },
                token: cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    private async Task ProcessBatchAsync(
        SubscriptionBatch<DiscoveryQueryStoredEventRecordDto> batch,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<KnownTrackRequestedHandler>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.Criteria, StringComparer.Ordinal))
        {
            var events = stream
                .OrderBy(item => item.Version)
                .Select(item => item.ToDomainEvent())
                .Where(item => item.Event is KnownTrackRequested)
                .ToArray();

            if (events.Length == 0)
            {
                continue;
            }

            await handler.Handle(
                new KnownTrackRequestedCommand(
                    DiscoveryQueryKey.ToKnownCatalogItem(stream.Key),
                    events),
                cancellationToken);
        }
    }
}
