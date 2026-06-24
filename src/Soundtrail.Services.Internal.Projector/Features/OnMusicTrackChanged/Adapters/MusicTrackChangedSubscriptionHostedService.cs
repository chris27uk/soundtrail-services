using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;

public sealed class MusicTrackChangedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ILogger<MusicTrackChangedSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "music-track-projections";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<MusicTrackStoredEventRecordDto>(options);
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
                logger.LogError(ex, "MusicTrack projection subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<MusicTrackStoredEventRecordDto>(
                new SubscriptionCreationOptions
                {
                    Name = SubscriptionName
                },
                token: cancellationToken);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async Task ProcessBatchAsync(
        SubscriptionBatch<MusicTrackStoredEventRecordDto> batch,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MusicTrackChangedHandler>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.MusicCatalogId, StringComparer.Ordinal))
        {
            var command = new MusicTrackChangedCommand(
                MusicCatalogId.From(stream.Key),
                stream.OrderBy(item => item.Version)
                    .Select(item => new VersionedMusicTrackEvent(item.Version, item.ToDomainEvent()))
                    .ToArray());

            await handler.Handle(command, cancellationToken);
        }
    }
}
