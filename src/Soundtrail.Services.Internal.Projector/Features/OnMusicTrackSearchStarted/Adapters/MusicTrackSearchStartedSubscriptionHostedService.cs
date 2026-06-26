using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Adapters;

public sealed class MusicTrackSearchStartedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ILogger<MusicTrackSearchStartedSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-search-started-projections";

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
                logger.LogError(ex, "Music track search started subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<DiscoveryQueryStoredEventRecordDto>(
                new SubscriptionCreationOptions
                {
                    Name = SubscriptionName
                },
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
        var handler = scope.ServiceProvider.GetRequiredService<MusicTrackSearchStartedHandler>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.Criteria, StringComparer.Ordinal))
        {
            var events = stream
                .OrderBy(item => item.Version)
                .Select(item => item.ToDomainEvent())
                .Where(item => item.Event is MusicTrackSearchStarted)
                .ToArray();

            if (events.Length == 0)
            {
                continue;
            }

            await handler.Handle(
                new MusicTrackSearchStartedCommand(
                    CatalogSearchCriteria.From(stream.Key),
                    events),
                cancellationToken);
        }
    }
}
