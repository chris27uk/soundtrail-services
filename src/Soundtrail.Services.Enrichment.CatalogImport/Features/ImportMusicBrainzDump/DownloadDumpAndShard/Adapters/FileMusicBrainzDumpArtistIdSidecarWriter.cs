using System.Text;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class FileMusicBrainzDumpArtistIdSidecarWriter : IMusicBrainzDumpArtistIdSidecarWriter
{
    private const int StreamBufferSize = 1024 * 64;

    private readonly StreamWriter[] writers;
    private bool closed;

    private FileMusicBrainzDumpArtistIdSidecarWriter(StreamWriter[] writers)
    {
        this.writers = writers;
    }

    public static FileMusicBrainzDumpArtistIdSidecarWriter Open(
        MusicBrainzDumpImportJobId jobId,
        int shardCount,
        string shardDirectory,
        bool append = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);

        var writers = new StreamWriter[shardCount];
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            for (var shardId = 0; shardId < shardCount; shardId++)
            {
                writers[shardId] = OpenWriter(jobId, shardId, shardDirectory, append, encoding);
            }
        }
        catch
        {
            foreach (var writer in writers)
            {
                writer?.Dispose();
            }

            throw;
        }

        return new FileMusicBrainzDumpArtistIdSidecarWriter(writers);
    }

    public static FileMusicBrainzDumpArtistIdSidecarWriter OpenSingle(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        string shardDirectory)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shardId);
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var writers = new StreamWriter[shardId + 1];
        writers[shardId] = OpenWriter(jobId, shardId, shardDirectory, append: false, encoding);
        return new FileMusicBrainzDumpArtistIdSidecarWriter(writers);
    }

    private static StreamWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        string shardDirectory,
        bool append,
        Encoding encoding)
    {
        var path = FileMusicBrainzDumpShardWriter.ArtistIdSidecarPath(shardDirectory, jobId, shardId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var stream = new FileStream(
            path,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            StreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return new StreamWriter(stream, encoding, StreamBufferSize);
    }

    public async Task AppendAsync(int shardId, string artistId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shardId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shardId, writers.Length);
        if (writers[shardId] is null)
        {
            throw new InvalidOperationException($"Artist id sidecar writer for shard {shardId} is not open.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(artistId);
        await writers[shardId].WriteLineAsync(artistId.AsMemory(), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        foreach (var writer in writers)
        {
            if (writer is not null)
            {
                await writer.DisposeAsync();
            }
        }
    }
}
