using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;

public sealed class MusicTrackChangedSubscriptionHostedService(
    IDocumentStore documentStore,
    IServiceScopeFactory scopeFactory,
    ILogger<MusicTrackChangedSubscriptionHostedService> logger,
    ITypeRegistry translator) : BackgroundService
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
                logger.LogError(ex, "MusicTrack projection subscription failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.CreateAsync<RavenStoredEventRecord>(
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
        SubscriptionBatch<RavenStoredEventRecord> batch,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<MusicTrackChangedHandler>();

        foreach (var stream in batch.Items
                     .Select(item => item.Result)
                     .GroupBy(item => item.StreamId, StringComparer.Ordinal))
        {
            var command = new MusicTrackChangedCommand(
                MusicCatalogId.From(stream.Key),
                stream.OrderBy(item => item.Version)
                    .Select(item => new VersionedMusicTrackEvent(
                        item.Version,
                        translator.ToDomainObject<IMusicTrackEvent>(
                            item.Body ?? throw new InvalidOperationException($"Stored event '{item.Id}' is missing a body."))))
                    .ToArray());

            await handler.Handle(command, cancellationToken);
        }
    }
}
