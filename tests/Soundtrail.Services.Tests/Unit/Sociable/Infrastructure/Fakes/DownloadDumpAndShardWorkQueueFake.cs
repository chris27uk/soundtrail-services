using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class DownloadDumpAndShardWorkQueueFake : IDownloadDumpAndShardWorkQueue
{
    private readonly List<DownloadDumpAndShardWork> pending = [];

    public IReadOnlyList<DownloadDumpAndShardWork> Pending => pending;

    public ValueTask EnqueueAsync(DownloadDumpAndShardWork work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        pending.Add(work);
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<DownloadDumpAndShardWork> ReadAllAsync(
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

    public IReadOnlyList<DownloadDumpAndShardWork> DequeueAll()
    {
        var items = pending.ToArray();
        pending.Clear();
        return items;
    }
}
