using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal abstract class RavenEventSubscriptionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : BackgroundService
{
    protected abstract string SubscriptionName { get; }

    protected abstract Expression<Func<RavenStoredEventRecord, bool>> Filter { get; }

    protected virtual int MaxDocsPerBatch => 128;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureSubscriptionExistsAsync(stoppingToken);

        var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenStoredEventRecord>(
            new SubscriptionWorkerOptions(SubscriptionName)
            {
                Strategy = SubscriptionOpeningStrategy.WaitForFree,
                MaxDocsPerBatch = MaxDocsPerBatch
            });

        await worker.Run(async batch =>
        {
            using var scope = scopeFactory.CreateScope();
            foreach (var item in batch.Items)
            {
                await HandleAsync(scope.ServiceProvider, item.Result, stoppingToken);
            }
        }, stoppingToken);
    }

    protected abstract Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken);

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await documentStore.Subscriptions.GetSubscriptionStateAsync(SubscriptionName, null, cancellationToken);
        }
        catch (SubscriptionDoesNotExistException)
        {
            await documentStore.Subscriptions.CreateAsync<RavenStoredEventRecord>(
                Filter,
                new PredicateSubscriptionCreationOptions
                {
                    Name = SubscriptionName
                },
                null,
                cancellationToken);
        }
    }
}
