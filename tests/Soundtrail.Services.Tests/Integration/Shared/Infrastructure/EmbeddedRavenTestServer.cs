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
    private static readonly object ServerSync = new();
    private static bool serverStarted;

    /// <summary>
    /// Creates a document store against the shared embedded Raven server.
    /// When <paramref name="databaseName"/> is omitted, a unique database is created so tests can run in parallel.
    /// </summary>
    public static IDocumentStore CreateDocumentStore(string? databaseName = null)
    {
        databaseName ??= $"soundtrail-tests-{Guid.NewGuid():N}";

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

    /// <summary>
    /// Deletes a single document. Use for mid-test resets within a long-lived environment.
    /// </summary>
    public static async ValueTask DeleteDocumentAsync(IDocumentStore documentStore, string documentId)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return;
        }

        try
        {
            using var session = documentStore.OpenAsyncSession();
            session.Advanced.Defer(new DeleteCommandData(documentId, null));
            await session.SaveChangesAsync();
        }
        catch
        {
        }
    }

    /// <summary>
    /// Deletes the store's database and disposes the store.
    /// </summary>
    public static ValueTask DisposeAsync(IDocumentStore? documentStore)
    {
        if (documentStore is null)
        {
            return ValueTask.CompletedTask;
        }

        var databaseName = documentStore.Database;
        try
        {
            documentStore.Maintenance.Server.Send(
                new DeleteDatabasesOperation(databaseName, hardDelete: true));
        }
        catch
        {
        }

        documentStore.Dispose();
        return ValueTask.CompletedTask;
    }

    private static void EnsureStarted()
    {
        if (Volatile.Read(ref serverStarted))
        {
            return;
        }

        lock (ServerSync)
        {
            if (serverStarted)
            {
                return;
            }

            try
            {
                EmbeddedServer.Instance.StartServer();
            }
            catch (InvalidOperationException exception)
                when (exception.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
            {
            }

            serverStarted = true;
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
