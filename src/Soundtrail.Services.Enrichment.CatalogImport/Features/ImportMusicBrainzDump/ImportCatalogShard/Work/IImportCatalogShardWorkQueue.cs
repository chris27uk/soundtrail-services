namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

public interface IImportCatalogShardWorkQueue
{
    ValueTask EnqueueAsync(ImportCatalogShardWork work, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ImportCatalogShardWork> ReadAllAsync(CancellationToken cancellationToken);
}
