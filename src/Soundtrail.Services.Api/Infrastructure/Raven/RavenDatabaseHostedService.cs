using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenDatabaseHostedService(IDocumentStore documentStore) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var record = new DatabaseRecord(documentStore.Database);

        try
        {
            await documentStore.Maintenance.Server.SendAsync(
                new CreateDatabaseOperation(record, replicationFactor: 1),
                cancellationToken);
        }
        catch (Exception ex) when (ex is ConcurrencyException or DatabaseDisabledException or RavenException)
        {
            // Another node/process is already creating or updating the database.
        }

        await WaitForDatabaseAvailabilityAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WaitForDatabaseAvailabilityAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await documentStore.Maintenance
                    .ForDatabase(documentStore.Database)
                    .SendAsync(new GetStatisticsOperation(), cancellationToken);

                return;
            }
            catch (Exception ex) when (ex is DatabaseDisabledException or DatabaseDoesNotExistException or RavenException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        await documentStore.Maintenance
            .ForDatabase(documentStore.Database)
            .SendAsync(new GetStatisticsOperation(), cancellationToken);
    }
}
