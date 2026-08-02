using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions.Documents.Subscriptions;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal abstract class RavenEventSubscriptionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore,
    ILogger<RavenEventSubscriptionBackgroundService>? logger = null) : BackgroundService
{
    private const string BeginningOfTimeChangeVector = "BeginningOfTime";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected abstract string SubscriptionName { get; }

    protected abstract Expression<Func<RavenStoredEventRecord, bool>> Filter { get; }

    protected virtual int MaxDocsPerBatch => 128;

    protected virtual bool IsSubscriptionDefinitionStale(SubscriptionState state) => false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureSubscriptionExistsAsync(stoppingToken);

                using var worker = documentStore.Subscriptions.GetSubscriptionWorker<RavenStoredEventRecord>(
                    new SubscriptionWorkerOptions(SubscriptionName)
                    {
                        Strategy = SubscriptionOpeningStrategy.WaitForFree,
                        MaxDocsPerBatch = MaxDocsPerBatch
                    });

                logger?.LogInformation("Raven subscription '{SubscriptionName}' is starting.", SubscriptionName);

                await worker.Run(async batch =>
                {
                    using var scope = scopeFactory.CreateScope();
                    foreach (var item in batch.Items)
                    {
                        await HandleAsync(scope.ServiceProvider, item.Result, stoppingToken);
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger?.LogError(
                    exception,
                    "Raven subscription '{SubscriptionName}' failed. It will retry in {RetryDelay}.",
                    SubscriptionName,
                    RetryDelay);

                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    protected abstract Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken);

    private async Task EnsureSubscriptionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var state = await documentStore.Subscriptions.GetSubscriptionStateAsync(SubscriptionName, null, cancellationToken);
            if (IsSubscriptionDefinitionStale(state))
            {
                await documentStore.Subscriptions.DeleteAsync(SubscriptionName, null, cancellationToken);
                await CreateSubscriptionAsync(cancellationToken);
            }
        }
        catch (SubscriptionDoesNotExistException)
        {
            await CreateSubscriptionAsync(cancellationToken);
        }
    }

    private async Task CreateSubscriptionAsync(CancellationToken cancellationToken)
    {
        await documentStore.Subscriptions.CreateAsync<RavenStoredEventRecord>(
            Filter,
            new PredicateSubscriptionCreationOptions
            {
                Name = SubscriptionName,
                ChangeVector = BeginningOfTimeChangeVector
            },
            null,
            cancellationToken);
    }
}
