using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

public sealed record DownloadDumpAndShardWork(MusicBrainzDumpImportJobId JobId);
