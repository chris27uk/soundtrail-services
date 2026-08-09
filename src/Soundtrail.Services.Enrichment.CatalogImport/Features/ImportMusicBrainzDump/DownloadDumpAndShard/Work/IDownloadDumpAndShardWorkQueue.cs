namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

public interface IDownloadDumpAndShardWorkQueue
{
    ValueTask EnqueueAsync(DownloadDumpAndShardWork work, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DownloadDumpAndShardWork> ReadAllAsync(CancellationToken cancellationToken);
}
