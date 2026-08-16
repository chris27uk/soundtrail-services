using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpArchiveStoreFake : IMusicBrainzDumpArchiveStore
{
    private readonly List<string> artistsJsonlLines = [];

    public IReadOnlyList<string> ArtistsJsonlLines => artistsJsonlLines;

    public MusicBrainzDumpArchiveStoreFake WithArtistsJsonl(params string[] lines)
    {
        artistsJsonlLines.Clear();
        artistsJsonlLines.AddRange(lines);
        return this;
    }

    public async Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        var path = Path.Combine(
            Path.GetTempPath(),
            "soundtrail-mb-fixture",
            $"{Guid.NewGuid():N}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllLinesAsync(path, artistsJsonlLines, cancellationToken);
        return path;
    }
}
