using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Embedded;

namespace Soundtrail.Services.Tests.Integration.Ports;

internal static class EmbeddedRavenTestServer
{
    private static int serverStarted;
    private static readonly Lazy<IDocumentStore> SharedStore = new(
        () =>
        {
            EnsureStarted();
            return EmbeddedServer.Instance.GetDocumentStore("soundtrail-services-tests");
        });

    public static IDocumentStore CreateDocumentStore() => SharedStore.Value;

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
}
