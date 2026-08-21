using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class BlobMusicBrainzDumpShardStore(
    IMusicBrainzDumpBlobContainer blobs,
    IOptions<MusicBrainzDumpOptions> options)
    : IMusicBrainzDumpShardStore
{
    public IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount) =>
        FileMusicBrainzDumpShardWriter.Open(
            jobId,
            phase,
            shardCount,
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
            async (paths, cancellationToken) =>
            {
                for (var shardId = 0; shardId < paths.Count; shardId++)
                {
                    await blobs.UploadFromFileAsync(
                        MusicBrainzDumpBlobKeys.Shard(jobId, phase, shardId),
                        paths[shardId],
                        cancellationToken);
                }

                // Keep local shard files for import reads — Azurite HTTP line streaming is much slower.
            });

    public async IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var localPath = FileMusicBrainzDumpShardWriter.ShardFilePath(
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
            jobId,
            phase,
            shardId);

        if (!File.Exists(localPath))
        {
            var blobName = MusicBrainzDumpBlobKeys.Shard(jobId, phase, shardId);
            if (await blobs.ExistsAsync(blobName, cancellationToken))
            {
                await blobs.DownloadToFileAsync(blobName, localPath, cancellationToken);
            }
        }

        if (!File.Exists(localPath))
        {
            yield break;
        }

        long lineNumber = 0;
        await foreach (var line in File.ReadLinesAsync(localPath, cancellationToken))
        {
            if (lineNumber++ < skipLines)
            {
                continue;
            }

            yield return line;
        }
    }
}
