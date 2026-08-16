using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpArchiveStoreFake : IMusicBrainzDumpArchiveStore
{
    private readonly List<string> artistsJsonlLines = [];
    private readonly List<string> releaseGroupsJsonlLines = [];

    public IReadOnlyList<string> ArtistsJsonlLines => artistsJsonlLines;

    public IReadOnlyList<string> ReleaseGroupsJsonlLines => releaseGroupsJsonlLines;

    public MusicBrainzDumpArchiveStoreFake WithArtistsJsonl(params string[] lines)
    {
        artistsJsonlLines.Clear();
        artistsJsonlLines.AddRange(lines);
        return this;
    }

    public MusicBrainzDumpArchiveStoreFake WithReleaseGroupsJsonl(params string[] lines)
    {
        releaseGroupsJsonlLines.Clear();
        releaseGroupsJsonlLines.AddRange(lines);
        return this;
    }

    public async Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        return await WriteTempAsync(artistsJsonlLines, cancellationToken);
    }

    public async Task<string> EnsureReleaseGroupsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        return await WriteTempAsync(releaseGroupsJsonlLines, cancellationToken);
    }

    private static async Task<string> WriteTempAsync(
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "soundtrail-mb-fixture",
            $"{Guid.NewGuid():N}.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllLinesAsync(path, lines, cancellationToken);
        return path;
    }
}
