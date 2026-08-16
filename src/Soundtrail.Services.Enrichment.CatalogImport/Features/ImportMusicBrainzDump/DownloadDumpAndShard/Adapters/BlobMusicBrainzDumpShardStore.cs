using System.Runtime.CompilerServices;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class BlobMusicBrainzDumpShardStore(IMusicBrainzDumpBlobContainer blobs)
    : IMusicBrainzDumpShardStore
{
    public Task WriteShardAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default) =>
        blobs.UploadLinesAsync(
            MusicBrainzDumpBlobKeys.Shard(jobId, phase, shardId),
            lines,
            cancellationToken);

    public async IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var line in blobs.ReadLinesAsync(
                           MusicBrainzDumpBlobKeys.Shard(jobId, phase, shardId),
                           skipLines,
                           cancellationToken))
        {
            yield return line;
        }
    }
}
