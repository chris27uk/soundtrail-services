using System.Text;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class FileMusicBrainzDumpShardWriter : IMusicBrainzDumpShardWriter
{
    private const int StreamBufferSize = 1024 * 64;

    private readonly StreamWriter[] writers;
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task>? completeAsync;
    private bool completed;
    private bool closed;
    private bool disposed;

    private FileMusicBrainzDumpShardWriter(
        StreamWriter[] writers,
        IReadOnlyList<string> paths,
        Func<IReadOnlyList<string>, CancellationToken, Task>? completeAsync)
    {
        this.writers = writers;
        Paths = paths;
        this.completeAsync = completeAsync;
    }

    public IReadOnlyList<string> Paths { get; }

    public static FileMusicBrainzDumpShardWriter Open(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount,
        string shardDirectory,
        Func<IReadOnlyList<string>, CancellationToken, Task>? completeAsync = null,
        bool append = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);

        var writers = new StreamWriter[shardCount];
        var paths = new string[shardCount];
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            for (var shardId = 0; shardId < shardCount; shardId++)
            {
                var path = ShardFilePath(shardDirectory, jobId, phase, shardId);
                paths[shardId] = path;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (append)
                {
                    TruncateTrailingPartialLine(path);
                }

                var stream = new FileStream(
                    path,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    StreamBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                writers[shardId] = new StreamWriter(stream, encoding, StreamBufferSize);
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

        return new FileMusicBrainzDumpShardWriter(writers, paths, completeAsync);
    }

    public static void TruncateTrailingPartialLine(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            StreamBufferSize,
            FileOptions.None);
        if (stream.Length == 0)
        {
            return;
        }

        stream.Seek(-1, SeekOrigin.End);
        if (stream.ReadByte() == '\n')
        {
            return;
        }

        long position = stream.Length - 1;
        while (position > 0)
        {
            stream.Seek(position - 1, SeekOrigin.Begin);
            if (stream.ReadByte() == '\n')
            {
                stream.SetLength(position);
                return;
            }

            position--;
        }

        stream.SetLength(0);
    }

    public async Task AppendAsync(int shardId, string line, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(shardId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shardId, writers.Length);
        ArgumentNullException.ThrowIfNull(line);

        await writers[shardId].WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (completed)
        {
            return;
        }

        await CloseWritersAsync();

        if (completeAsync is not null)
        {
            await completeAsync(Paths, cancellationToken);
        }

        completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await CloseWritersAsync();
    }

    private async Task CloseWritersAsync()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        foreach (var writer in writers)
        {
            await writer.DisposeAsync();
        }
    }

    public static string ShardFilePath(
        string shardDirectory,
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId)
    {
        var safeJob = jobId.Value.Replace(':', '_');
        return Path.Combine(shardDirectory, safeJob, phase.ToString(), $"{shardId}.jsonl");
    }

    /// <summary>
    /// Compact one-id-per-line sidecar beside the Artists JSONL (avoids re-reading multi-GB dumps).
    /// </summary>
    public static string ArtistIdSidecarPath(
        string shardDirectory,
        MusicBrainzDumpImportJobId jobId,
        int shardId)
    {
        var safeJob = jobId.Value.Replace(':', '_');
        return Path.Combine(
            shardDirectory,
            safeJob,
            MusicBrainzDumpImportPhase.Artists.ToString(),
            $"{shardId}.ids");
    }

    public static string ResolveShardDirectory(string? configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "soundtrail-mb-shards")
            : configured;
}
