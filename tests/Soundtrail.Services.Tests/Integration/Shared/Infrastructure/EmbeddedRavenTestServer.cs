using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Conventions;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

internal static class EmbeddedRavenTestServer
{
    private const string DefaultDatabaseName = "soundtrail-services-tests";
    private static int serverStarted;

    public static IDocumentStore CreateDocumentStore(string databaseName = DefaultDatabaseName)
    {
        EnsureStarted();
        var serverUri = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult();
        var store = new DocumentStore
        {
            Urls = [serverUri.AbsoluteUri.TrimEnd('/')],
            Database = databaseName,
            Conventions = new DocumentConventions
            {
                FindCollectionName = type => type.Name
            }
        };

        store.Initialize();
        EnsureDatabaseExists(store);
        return store;
    }

    public static async Task<string> GetServerUrlAsync()
    {
        EnsureStarted();
        var serverUri = await EmbeddedServer.Instance.GetServerUriAsync();
        return serverUri.AbsoluteUri.TrimEnd('/');
    }

    public static async ValueTask DisposeAsync(IDocumentStore? documentStore, string? documentId)
    {
        if (documentStore is null)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(documentId))
            {
                using var session = documentStore.OpenAsyncSession();
                session.Advanced.Defer(new DeleteCommandData(documentId, null));
                await session.SaveChangesAsync();
            }
        }
        catch
        {
        }
    }

    private static void EnsureStarted()
    {
        if (Interlocked.Exchange(ref serverStarted, 1) == 1)
        {
            return;
        }

        try
        {
            EmbeddedServer.Instance.StartServer();
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static void EnsureDatabaseExists(IDocumentStore documentStore)
    {
        try
        {
            documentStore.Maintenance.Server.Send(
                new CreateDatabaseOperation(new DatabaseRecord(documentStore.Database), replicationFactor: 1));
        }
        catch (Exception exception) when (exception is ConcurrencyException or DatabaseDisabledException or RavenException)
        {
        }
    }
}
