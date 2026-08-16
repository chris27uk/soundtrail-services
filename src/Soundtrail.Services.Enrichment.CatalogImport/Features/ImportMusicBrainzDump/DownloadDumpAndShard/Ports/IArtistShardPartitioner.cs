namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

public interface IArtistShardPartitioner
{
    int ShardIdFor(string artistKey, int shardCount);
}
