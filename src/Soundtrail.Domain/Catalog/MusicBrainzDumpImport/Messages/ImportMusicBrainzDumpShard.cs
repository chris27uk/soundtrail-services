using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;

public sealed record ImportMusicBrainzDumpShard(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset RequestedAt,
    MusicBrainzDumpImportJobId JobId,
    MusicBrainzDumpImportPhase Phase,
    int ShardId) : IMessage
{
    public static ImportMusicBrainzDumpShard Create(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        DateTimeOffset requestedAt)
    {
        if (shardId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardId));
        }

        return new ImportMusicBrainzDumpShard(
            MessageId.For($"mb-dump-shard:{jobId.Value}:{phase}:{shardId}"),
            CorrelationId.From(jobId.Value),
            requestedAt,
            jobId,
            phase,
            shardId);
    }
}
