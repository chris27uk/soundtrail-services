using System.Runtime.CompilerServices;
using System.Text;
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
        int shardCount,
        bool append = false) =>
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
            },
            append);

    public async IAsyncEnumerable<MusicBrainzDumpShardLine> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        long skipBytes = 0,
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
                if (ShouldStreamShardLinesFromBlob())
                {
                    // Blob HTTP streaming has no cheap byte seek; fall back to line discard.
                    var lineNumber = 0L;
                    var endByteOffset = 0L;
                    await foreach (var line in blobs.ReadLinesAsync(blobName, skipLines: 0, cancellationToken))
                    {
                        endByteOffset += Encoding.UTF8.GetByteCount(line) + 1;
                        if (lineNumber++ < skipLines)
                        {
                            continue;
                        }

                        yield return new MusicBrainzDumpShardLine(line, endByteOffset);
                    }

                    yield break;
                }

                await blobs.DownloadToFileAsync(blobName, localPath, cancellationToken);
            }
        }

        if (!File.Exists(localPath))
        {
            yield break;
        }

        await foreach (var line in MusicBrainzDumpShardFileReader.ReadLinesAsync(
                           localPath,
                           skipLines,
                           skipBytes,
                           cancellationToken))
        {
            yield return line;
        }
    }

    public IMusicBrainzDumpArtistIdSidecarWriter OpenArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardCount,
        bool append = false) =>
        FileMusicBrainzDumpArtistIdSidecarWriter.Open(
            jobId,
            shardCount,
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
            append);

    public IMusicBrainzDumpArtistIdSidecarWriter OpenSingleArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardId) =>
        FileMusicBrainzDumpArtistIdSidecarWriter.OpenSingle(
            jobId,
            shardId,
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory));

    public async IAsyncEnumerable<string> ReadArtistIdSidecarAsync(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = FileMusicBrainzDumpShardWriter.ArtistIdSidecarPath(
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
            jobId,
            shardId);
        if (!File.Exists(path))
        {
            yield break;
        }

        await foreach (var line in File.ReadLinesAsync(path, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                yield return line.Trim();
            }
        }
    }

    public bool ArtistIdSidecarExists(MusicBrainzDumpImportJobId jobId, int shardId) =>
        File.Exists(
            FileMusicBrainzDumpShardWriter.ArtistIdSidecarPath(
                FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
                jobId,
                shardId));

    private bool ShouldStreamShardLinesFromBlob()
    {
        if (options.Value.StreamShardLinesFromBlob is { } configured)
        {
            return configured;
        }

        return !MusicBrainzDumpBlobHost.IsDevelopmentStorage(options.Value.BlobConnectionString);
    }
}
