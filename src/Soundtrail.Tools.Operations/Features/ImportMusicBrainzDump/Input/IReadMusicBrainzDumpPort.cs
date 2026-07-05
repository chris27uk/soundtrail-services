using Soundtrail.Domain.Catalog;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;

public interface IReadMusicBrainzDumpPort
{
    IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
        IReadOnlyList<string> recordingDumpPaths,
        IReadOnlyList<string> releaseDumpPaths,
        CancellationToken cancellationToken);
}
