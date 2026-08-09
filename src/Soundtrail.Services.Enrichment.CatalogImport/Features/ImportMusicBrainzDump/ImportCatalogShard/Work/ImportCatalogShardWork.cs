using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

public sealed record ImportCatalogShardWork(
    MusicBrainzDumpImportJobId JobId,
    MusicBrainzDumpImportPhase Phase,
    int ShardId);
