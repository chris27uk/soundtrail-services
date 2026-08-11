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

    /// <summary>
    /// Stable E2E database name. Prefer this over per-run Guid names so Embedded DataDir
    /// does not accumulate databases (and Raven Community's 15 subscriptions/cluster).
    /// </summary>
    public const string EndToEndDatabaseName = "soundtrail-e2e";

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
    /// Starts embedded Raven during assembly warmup so first integration test does not pay cold-start cost.
    /// </summary>
    public static void EnsureServerStarted() => _ = GetReadyServerUrl();

    /// <summary>
    /// Stops the shared store and embedded server so MTP does not wait on leftover foreground threads.
    /// </summary>
    public static async ValueTask ShutdownAsync()
    {
        IDocumentStore? store;
        lock (ServerSync)
        {
            store = sharedStore;
            sharedStore = null;
            serverUrl = null;
        }

        try
        {
            store?.Dispose();
        }
        catch
        {
        }

        try
        {
            await EmbeddedServer.Instance.StopServerAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

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

            // Embedded persists under bin/.../RavenDB across testhost runs. Shared Lazy E2E
            // never disposes its store, and older Guid-named DBs left subscriptions that
            // hit Community's 15-per-cluster limit. Wipe orphans before any CreateAsync.
            DeleteOrphanedTestDatabases(url);

            // Publish only after the server reports a URI (init finished). Parallel
            // callers then share this URL instead of racing StartServer/FirstTopologyUpdate.
            Volatile.Write(ref serverUrl, url);
            TestContainerWarmup.EnsureServiceBusWarmupStarted();
            return url;
        }
    }

    /// <summary>
    /// Drops leftover E2E / isolated test databases (and their subscriptions) while keeping
    /// the shared integration database and the stable E2E database name (wiped separately
    /// when E2E hosts start).
    /// </summary>
    public static void DeleteOrphanedTestDatabases(string? serverUrlOverride = null)
    {
        var url = serverUrlOverride ?? Volatile.Read(ref serverUrl);
        if (url is null)
        {
            return;
        }

        try
        {
            using var adminStore = new DocumentStore { Urls = [url] }.Initialize();
            var names = adminStore.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 1024));
            foreach (var name in names.Where(IsOrphanedTestDatabase))
            {
                try
                {
                    adminStore.Maintenance.Server.Send(
                        new DeleteDatabasesOperation(name, hardDelete: true));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        // Folders left from crashed runs may remount on the next Embedded start even if
        // they were not in GetDatabaseNamesOperation — remove them from DataDir too.
        DeleteOrphanedDatabaseDirectories();
    }

    /// <summary>
    /// Ensures the stable E2E database is absent so Shared Lazy hosts start from a clean slate.
    /// </summary>
    public static void DeleteEndToEndDatabase()
    {
        var url = Volatile.Read(ref serverUrl) ?? GetReadyServerUrl();
        try
        {
            using var adminStore = new DocumentStore { Urls = [url] }.Initialize();
            adminStore.Maintenance.Server.Send(
                new DeleteDatabasesOperation(EndToEndDatabaseName, hardDelete: true));
        }
        catch
        {
        }

        try
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "RavenDB", "Databases", EndToEndDatabaseName);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static void DeleteOrphanedDatabaseDirectories()
    {
        try
        {
            var databasesRoot = Path.Combine(AppContext.BaseDirectory, "RavenDB", "Databases");
            if (!Directory.Exists(databasesRoot))
            {
                return;
            }

            foreach (var directory in Directory.EnumerateDirectories(databasesRoot))
            {
                var name = Path.GetFileName(directory);
                if (!IsOrphanedTestDatabase(name))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private static bool IsOrphanedTestDatabase(string name)
    {
        if (string.Equals(name, SharedDatabaseName, StringComparison.Ordinal)
            || string.Equals(name, EndToEndDatabaseName, StringComparison.Ordinal))
        {
            return false;
        }

        // Legacy Guid E2E DBs and any isolated/legacy soundtrail-test* DB.
        return name.StartsWith("soundtrail-e2e-", StringComparison.Ordinal)
            || name.StartsWith("soundtrail-test", StringComparison.Ordinal);
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
