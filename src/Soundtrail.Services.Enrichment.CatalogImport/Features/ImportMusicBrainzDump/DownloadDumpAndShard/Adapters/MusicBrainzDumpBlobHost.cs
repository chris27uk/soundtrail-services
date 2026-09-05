namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

internal static class MusicBrainzDumpBlobHost
{
    public static bool IsDevelopmentStorage(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return true;
        }

        return connectionString.Contains("UseDevelopmentStorage", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("devstoreaccount1", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("127.0.0.1:10000", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains("localhost:10000", StringComparison.OrdinalIgnoreCase);
    }
}
