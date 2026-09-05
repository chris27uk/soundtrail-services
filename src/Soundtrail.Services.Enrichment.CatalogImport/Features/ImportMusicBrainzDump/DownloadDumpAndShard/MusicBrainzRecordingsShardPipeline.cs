using System.Threading.Channels;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;

/// <summary>
/// Parallel release-line join into shard writes for the Recordings producer.
/// Decode/read stays single-threaded; JSON join fans out across workers; shard appends stay single-writer.
/// </summary>
public static class MusicBrainzRecordingsShardPipeline
{
    private readonly record struct ShardLine(int ShardId, string Line);

    private readonly record struct ReleaseDone(long ReleaseIndex);

    public sealed record Progress(
        long InputLinesCompleted,
        long TrackRows,
        long CopiedRows);

    public static async Task<Progress> ProcessReleaseGraphAsync(
        IAsyncEnumerable<string> releaseLines,
        long skipReleaseLines,
        int joinDegreeOfParallelism,
        int shardCount,
        IArtistShardPartitioner partitioner,
        IMusicBrainzDumpShardWriter writer,
        Func<Progress, CancellationToken, ValueTask>? onProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(releaseLines);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfNegative(skipReleaseLines);
        ArgumentOutOfRangeException.ThrowIfLessThan(joinDegreeOfParallelism, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);

        var dop = joinDegreeOfParallelism;
        var input = Channel.CreateBounded<(long Index, string Line)>(
            new BoundedChannelOptions(dop * 4)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false
            });
        var output = Channel.CreateBounded<object>(
            new BoundedChannelOptions(dop * 32)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            });

        long trackRows = 0;
        long copiedRows = 0;
        long contiguousCompleted = skipReleaseLines;
        var completedReleases = new SortedSet<long>();

        var producerTask = Task.Run(
            async () =>
            {
                try
                {
                    long index = 0;
                    await foreach (var line in releaseLines.WithCancellation(cancellationToken))
                    {
                        var current = index++;
                        if (current < skipReleaseLines)
                        {
                            continue;
                        }

                        await input.Writer.WriteAsync((current, line), cancellationToken);
                    }

                    input.Writer.Complete();
                }
                catch (Exception exception)
                {
                    input.Writer.TryComplete(exception);
                    throw;
                }
            },
            cancellationToken);

        var workerTasks = Enumerable
            .Range(0, dop)
            .Select(_ => Task.Run(
                async () =>
                {
                    await foreach (var (releaseIndex, releaseLine) in input.Reader.ReadAllAsync(cancellationToken))
                    {
                        foreach (var emission in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackEmissions(releaseLine))
                        {
                            Interlocked.Increment(ref trackRows);
                            foreach (var artistId in emission.CreditedArtistIds)
                            {
                                var shardId = partitioner.ShardIdFor(artistId, shardCount);
                                var wrapped = MusicBrainzTrackJsonLine.WrapForCreditedArtist(
                                    artistId,
                                    emission.JsonLine);
                                Interlocked.Increment(ref copiedRows);
                                await output.Writer.WriteAsync(new ShardLine(shardId, wrapped), cancellationToken);
                            }
                        }

                        await output.Writer.WriteAsync(new ReleaseDone(releaseIndex), cancellationToken);
                    }
                },
                cancellationToken))
            .ToArray();

        var workersCompletion = Task.Run(
            async () =>
            {
                try
                {
                    await Task.WhenAll(workerTasks);
                    output.Writer.Complete();
                }
                catch (Exception exception)
                {
                    output.Writer.TryComplete(exception);
                    throw;
                }
            },
            cancellationToken);

        var lastProgress = DateTime.UtcNow;
        await foreach (var item in output.Reader.ReadAllAsync(cancellationToken))
        {
            switch (item)
            {
                case ShardLine shardLine:
                    await writer.AppendAsync(shardLine.ShardId, shardLine.Line, cancellationToken);
                    break;
                case ReleaseDone done:
                    completedReleases.Add(done.ReleaseIndex);
                    while (completedReleases.Remove(contiguousCompleted))
                    {
                        contiguousCompleted++;
                    }

                    if (onProgress is not null &&
                        (DateTime.UtcNow - lastProgress >= TimeSpan.FromSeconds(5) ||
                         contiguousCompleted % 1_000 == 0))
                    {
                        lastProgress = DateTime.UtcNow;
                        await onProgress(
                            new Progress(
                                contiguousCompleted,
                                Interlocked.Read(ref trackRows),
                                Interlocked.Read(ref copiedRows)),
                            cancellationToken);
                    }

                    break;
            }
        }

        await producerTask;
        await workersCompletion;

        var final = new Progress(
            contiguousCompleted,
            Interlocked.Read(ref trackRows),
            Interlocked.Read(ref copiedRows));
        if (onProgress is not null)
        {
            await onProgress(final, cancellationToken);
        }

        return final;
    }

    public static async Task<Progress> ProcessTrackLinesAsync(
        IAsyncEnumerable<string> trackLines,
        long skipTrackLines,
        int shardCount,
        IArtistShardPartitioner partitioner,
        IMusicBrainzDumpShardWriter writer,
        Func<Progress, CancellationToken, ValueTask>? onProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trackLines);
        ArgumentNullException.ThrowIfNull(partitioner);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentOutOfRangeException.ThrowIfNegative(skipTrackLines);
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);

        long inputLines = 0;
        long trackRows = 0;
        long copiedRows = 0;
        var lastProgress = DateTime.UtcNow;

        await foreach (var line in trackLines.WithCancellation(cancellationToken))
        {
            var current = inputLines++;
            if (current < skipTrackLines)
            {
                continue;
            }

            trackRows++;
            if (!MusicBrainzTrackJsonLine.TryReadCreditedArtistIds(line, out var artistIds))
            {
                continue;
            }

            foreach (var artistId in artistIds)
            {
                await writer.AppendAsync(
                    partitioner.ShardIdFor(artistId, shardCount),
                    MusicBrainzTrackJsonLine.WrapForCreditedArtist(artistId, line),
                    cancellationToken);
                copiedRows++;
            }

            if (onProgress is not null &&
                (DateTime.UtcNow - lastProgress >= TimeSpan.FromSeconds(5) ||
                 inputLines % 10_000 == 0))
            {
                lastProgress = DateTime.UtcNow;
                await onProgress(new Progress(inputLines, trackRows, copiedRows), cancellationToken);
            }
        }

        var final = new Progress(inputLines, trackRows, copiedRows);
        if (onProgress is not null)
        {
            await onProgress(final, cancellationToken);
        }

        return final;
    }
}
