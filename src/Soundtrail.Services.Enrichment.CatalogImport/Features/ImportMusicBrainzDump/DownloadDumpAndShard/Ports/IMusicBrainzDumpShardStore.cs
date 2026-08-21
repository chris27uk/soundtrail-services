using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IMusicBrainzDumpShardStore
{
    IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount);

    IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        CancellationToken cancellationToken = default);
}

public interface IMusicBrainzDumpShardWriter : IAsyncDisposable
{
    Task AppendAsync(int shardId, string line, CancellationToken cancellationToken = default);

    Task CompleteAsync(CancellationToken cancellationToken = default);
}
