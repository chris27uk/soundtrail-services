using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

public sealed class ProjectMusicTrackCatalogSubscriptionHostedService(
    IDocumentStore documentStore,
    ProjectMusicTrackCatalogHandler handler,
    ILogger<ProjectMusicTrackCatalogSubscriptionHostedService> logger) : BackgroundService
{
    private const string SubscriptionName = "catalog-music-track-projections";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = new SubscriptionWorkerOptions(SubscriptionName);
                await using var worker = documentStore.Subscriptions.GetSubscriptionWorker<MusicTrackStoredEventRecordDto>(options);
                await worker.Run(
                    batch => handler.HandleAsync(
                        batch.Items.Select(item => item.Result).ToArray(),
                        stoppingToken),
                    stoppingToken);
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
                logger.LogError(ex, "Catalog projection subscription failed.");
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
}
