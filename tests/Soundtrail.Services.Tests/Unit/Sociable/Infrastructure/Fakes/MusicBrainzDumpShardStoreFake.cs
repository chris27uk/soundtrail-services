using System.Runtime.CompilerServices;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpShardStoreFake : IMusicBrainzDumpShardStore
{
    private readonly Dictionary<(string JobId, MusicBrainzDumpImportPhase Phase, int ShardId), List<string>> shards = new();

    public IReadOnlyDictionary<(string JobId, MusicBrainzDumpImportPhase Phase, int ShardId), IReadOnlyList<string>> Shards =>
        shards.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value);

    public IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            shards[(jobId.Value, phase, shardId)] = [];
        }

        return new Writer(this, jobId.Value, phase);
    }

    public async IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (!shards.TryGetValue((jobId.Value, phase, shardId), out var lines))
        {
            yield break;
        }

        for (var index = 0; index < lines.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (index < skipLines)
            {
                continue;
            }

            yield return lines[index];
        }
    }

    private sealed class Writer(
        MusicBrainzDumpShardStoreFake store,
        string jobId,
        MusicBrainzDumpImportPhase phase) : IMusicBrainzDumpShardWriter
    {
        public Task AppendAsync(int shardId, string line, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.shards[(jobId, phase, shardId)].Add(line);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
