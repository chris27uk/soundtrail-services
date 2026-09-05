using System.Runtime.CompilerServices;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpShardStoreFake : IMusicBrainzDumpShardStore
{
    private readonly Dictionary<(string JobId, MusicBrainzDumpImportPhase Phase, int ShardId), List<string>> shards = new();
    private readonly Dictionary<(string JobId, int ShardId), List<string>> artistIdSidecars = new();

    public IReadOnlyDictionary<(string JobId, MusicBrainzDumpImportPhase Phase, int ShardId), IReadOnlyList<string>> Shards =>
        shards.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<string>)pair.Value);

    public IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount,
        bool append = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            var key = (jobId.Value, phase, shardId);
            if (!append || !shards.ContainsKey(key))
            {
                shards[key] = [];
            }
        }

        return new Writer(this, jobId.Value, phase);
    }

    public async IAsyncEnumerable<MusicBrainzDumpShardLine> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        long skipBytes = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (!shards.TryGetValue((jobId.Value, phase, shardId), out var lines))
        {
            yield break;
        }

        var endByteOffset = 0L;
        for (var index = 0; index < lines.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = lines[index];
            endByteOffset += line.Length + 1;
            if (index < skipLines)
            {
                continue;
            }

            if (skipBytes > 0 && endByteOffset <= skipBytes)
            {
                continue;
            }

            yield return new MusicBrainzDumpShardLine(line, endByteOffset);
        }
    }

    public IMusicBrainzDumpArtistIdSidecarWriter OpenArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardCount,
        bool append = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);
        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            var key = (jobId.Value, shardId);
            if (!append || !artistIdSidecars.ContainsKey(key))
            {
                artistIdSidecars[key] = [];
            }
        }

        return new SidecarWriter(this, jobId.Value);
    }

    public IMusicBrainzDumpArtistIdSidecarWriter OpenSingleArtistIdSidecarWriter(
        MusicBrainzDumpImportJobId jobId,
        int shardId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(shardId);
        artistIdSidecars[(jobId.Value, shardId)] = [];
        return new SidecarWriter(this, jobId.Value);
    }

    public async IAsyncEnumerable<string> ReadArtistIdSidecarAsync(
        MusicBrainzDumpImportJobId jobId,
        int shardId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (!artistIdSidecars.TryGetValue((jobId.Value, shardId), out var ids))
        {
            yield break;
        }

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return id;
        }
    }

    public bool ArtistIdSidecarExists(MusicBrainzDumpImportJobId jobId, int shardId) =>
        artistIdSidecars.TryGetValue((jobId.Value, shardId), out var ids) && ids.Count > 0;

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

    private sealed class SidecarWriter(MusicBrainzDumpShardStoreFake store, string jobId)
        : IMusicBrainzDumpArtistIdSidecarWriter
    {
        public Task AppendAsync(int shardId, string artistId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            store.artistIdSidecars[(jobId, shardId)].Add(artistId);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
