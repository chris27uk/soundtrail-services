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
    private const string SharedDatabaseName = "soundtrail-tests";

    private static readonly object ServerSync = new();
    private static string? serverUrl;
    private static IDocumentStore? sharedStore;

    /// <summary>
    /// Returns the shared document store against the shared embedded Raven database.
    /// Tests isolate via unique document/entity ids, not per-test databases.
    /// When <paramref name="databaseName"/> is provided, creates an isolated store (escape hatch).
    /// </summary>
    public static IDocumentStore CreateDocumentStore(string? databaseName = null)
    {
        if (databaseName is not null)
        {
            return CreateIsolatedDocumentStore(databaseName);
        }

        var existing = Volatile.Read(ref sharedStore);
        if (existing is not null)
        {
            return existing;
        }

        lock (ServerSync)
        {
            if (sharedStore is not null)
            {
                return sharedStore;
            }

            var store = new DocumentStore
            {
                Urls = [GetReadyServerUrl()],
                Database = SharedDatabaseName,
                Conventions = new DocumentConventions
                {
                    FindCollectionName = type => type.Name
                }
            };

            store.Initialize();
            EnsureDatabaseExists(store);
            Volatile.Write(ref sharedStore, store);
            return store;
        }
    }

    public static Task<string> GetServerUrlAsync() => Task.FromResult(GetReadyServerUrl());

    /// <summary>
    /// Unique key for entity/document ids so parallel tests sharing one DB do not collide.
    /// </summary>
    public static string NewIsolationKey() => Guid.NewGuid().ToString("N");

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
    /// Deletes the listed documents from the store (best-effort). Prefer this over dropping databases.
    /// </summary>
    public static async ValueTask DeleteDocumentsAsync(
        IDocumentStore documentStore,
        IEnumerable<string> documentIds)
    {
        ArgumentNullException.ThrowIfNull(documentStore);
        ArgumentNullException.ThrowIfNull(documentIds);

        var ids = documentIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        try
        {
            using var session = documentStore.OpenAsyncSession();
            foreach (var documentId in ids)
            {
                session.Advanced.Defer(new DeleteCommandData(documentId, null));
            }

            await session.SaveChangesAsync();
        }
        catch
        {
        }
    }

    /// <summary>
    /// For the shared store: no-op (do not delete the database or dispose the store).
    /// For isolated stores: deletes the database and disposes the store.
    /// </summary>
    public static ValueTask DisposeAsync(IDocumentStore? documentStore)
    {
        if (documentStore is null || ReferenceEquals(documentStore, Volatile.Read(ref sharedStore)))
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

    private static IDocumentStore CreateIsolatedDocumentStore(string databaseName)
    {
        var store = new DocumentStore
        {
            Urls = [GetReadyServerUrl()],
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

    /// <summary>
    /// StartServer returns before the HTTP endpoint is ready for FirstTopologyUpdate.
    /// Only publish the URL after GetServerUriAsync completes so parallel callers never
    /// Initialize a DocumentStore against a still-booting process.
    /// </summary>
    private static string GetReadyServerUrl()
    {
        var existing = Volatile.Read(ref serverUrl);
        if (existing is not null)
        {
            return existing;
        }

        lock (ServerSync)
        {
            if (serverUrl is not null)
            {
                return serverUrl;
            }

            try
            {
                EmbeddedServer.Instance.StartServer();
            }
            catch (InvalidOperationException exception)
                when (exception.Message.Contains("already started", StringComparison.OrdinalIgnoreCase))
            {
            }

            var serverUri = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult();
            var url = serverUri.AbsoluteUri.TrimEnd('/');

            // Publish only after the server reports a URI (init finished). Parallel
            // callers then share this URL instead of racing StartServer/FirstTopologyUpdate.
            Volatile.Write(ref serverUrl, url);
            TestContainerWarmup.EnsureServiceBusWarmupStarted();
            return url;
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
