using System.Text;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

internal sealed class FileMusicBrainzDumpShardWriter : IMusicBrainzDumpShardWriter
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
        Func<IReadOnlyList<string>, CancellationToken, Task>? completeAsync = null)
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
                var stream = new FileStream(
                    path,
                    FileMode.Create,
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

    public static string ResolveShardDirectory(string? configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "soundtrail-mb-shards")
            : configured;
}
