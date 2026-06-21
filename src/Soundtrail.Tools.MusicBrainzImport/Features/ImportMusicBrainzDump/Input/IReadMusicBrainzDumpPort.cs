using Soundtrail.Domain.Model;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;

public interface IReadMusicBrainzDumpPort
{
    IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
        IReadOnlyList<string> recordingDumpPaths,
        IReadOnlyList<string> releaseDumpPaths,
        CancellationToken cancellationToken);
}
