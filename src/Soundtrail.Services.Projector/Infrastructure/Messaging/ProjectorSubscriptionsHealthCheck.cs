using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents;
using Raven.Client.Exceptions.Documents.Subscriptions;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class ProjectorSubscriptionsHealthCheck(IDocumentStore documentStore) : IHealthCheck
{
    private static readonly string[] RequiredSubscriptions =
    [
        "projector/catalog",
        "projector/discovery"
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));

        var missingSubscriptions = new List<string>();

        try
        {
            foreach (var subscriptionName in RequiredSubscriptions)
            {
                try
                {
                    await documentStore.Subscriptions.GetSubscriptionStateAsync(
                        subscriptionName,
                        null,
                        timeout.Token);
                }
                catch (SubscriptionDoesNotExistException)
                {
                    missingSubscriptions.Add(subscriptionName);
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                "Projector subscriptions could not be verified.",
                exception);
        }

        if (missingSubscriptions.Count == 0)
        {
            return HealthCheckResult.Healthy("Projector subscriptions are available.");
        }

        return HealthCheckResult.Unhealthy(
            $"Missing projector subscription(s): {string.Join(", ", missingSubscriptions)}.");
    }
}
