using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class ArtistShardPartitioner : IArtistShardPartitioner
{
    public int ShardIdFor(string artistKey, int shardCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artistKey);
        if (shardCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardCount), shardCount, "Shard count must be positive.");
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(artistKey.Trim()));
        var value = BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(0, 4));
        return (int)(value % (uint)shardCount);
    }
}
