using Raven.Client.Exceptions;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump;

internal static class MusicBrainzDumpImportJobConcurrency
{
    public const int SaveAttempts = 5;

    public static bool IsConflict(Exception exception) =>
        exception is ConcurrencyException ||
        exception.GetBaseException() is ConcurrencyException ||
        exception.InnerException is ConcurrencyException;
}
