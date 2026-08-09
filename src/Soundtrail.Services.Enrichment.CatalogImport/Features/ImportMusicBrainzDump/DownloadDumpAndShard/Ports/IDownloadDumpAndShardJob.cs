using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IDownloadDumpAndShardJob
{
    Task RunAsync(MusicBrainzDumpImportJobId jobId, CancellationToken cancellationToken = default);
}
