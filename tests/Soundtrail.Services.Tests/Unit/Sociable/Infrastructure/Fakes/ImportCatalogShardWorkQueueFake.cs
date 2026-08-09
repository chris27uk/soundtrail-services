using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class ImportCatalogShardWorkQueueFake : IImportCatalogShardWorkQueue
{
    private readonly List<ImportCatalogShardWork> pending = [];

    public IReadOnlyList<ImportCatalogShardWork> Pending => pending;

    public ValueTask EnqueueAsync(ImportCatalogShardWork work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        pending.Add(work);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<ImportCatalogShardWork> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = pending[0];
            pending.RemoveAt(0);
            yield return next;
            await Task.Yield();
        }
    }

    public IReadOnlyList<ImportCatalogShardWork> DequeueAll()
    {
        var items = pending.ToArray();
        pending.Clear();
        return items;
    }
}
