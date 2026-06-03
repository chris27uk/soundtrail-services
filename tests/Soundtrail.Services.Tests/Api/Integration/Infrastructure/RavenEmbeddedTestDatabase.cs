using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Embedded;

namespace Soundtrail.Services.Tests.Api.Integration.Infrastructure;

internal sealed class RavenEmbeddedTestDatabase : IDisposable
{
    private static readonly object Sync = new();
    private static string? dataDirectory;
    private static bool started;

    private readonly IDocumentStore store;

    private RavenEmbeddedTestDatabase(IDocumentStore store)
    {
        this.store = store;
    }

    public IDocumentStore Store => this.store;

    public static RavenEmbeddedTestDatabase Create()
    {
        EnsureStarted();

        var databaseName = $"soundtrail-tests-{Guid.NewGuid():N}";
        var store = EmbeddedServer.Instance.GetDocumentStore(new DatabaseOptions(databaseName)
        {
            Conventions = new DocumentConventions
            {
                FindCollectionName = type => type.Name
            }
        });
        return new RavenEmbeddedTestDatabase(store);
    }

    public void Dispose()
    {
        this.store.Dispose();
    }

    private static void EnsureStarted()
    {
        lock (Sync)
        {
            if (started)
            {
                return;
            }

            dataDirectory = Path.Combine(
                Path.GetTempPath(),
                "soundtrail-raven-tests",
                Guid.NewGuid().ToString("N"));

            Environment.SetEnvironmentVariable("RAVEN_ARGS", $"--ServerUrl=http://127.0.0.1:0 --DataDir=\"{dataDirectory}\"");
            EmbeddedServer.Instance.StartServer();

            started = true;
        }
    }
}
