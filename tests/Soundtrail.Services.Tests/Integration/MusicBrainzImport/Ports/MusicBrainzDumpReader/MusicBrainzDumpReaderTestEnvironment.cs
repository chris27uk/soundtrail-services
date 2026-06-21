using Soundtrail.Domain.Model;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

namespace Soundtrail.Services.Tests.Integration.MusicBrainzImport.Ports.MusicBrainzDumpReader;

internal sealed class MusicBrainzDumpReaderTestEnvironment : IDisposable
{
    private MusicBrainzDumpReaderTestEnvironment(
        IReadMusicBrainzDumpPort port,
        IReadOnlyList<string> recordingDumpPaths,
        IReadOnlyList<string> releaseDumpPaths,
        string? tempDirectory)
    {
        Port = port;
        RecordingDumpPaths = recordingDumpPaths;
        ReleaseDumpPaths = releaseDumpPaths;
        this.tempDirectory = tempDirectory;
    }

    private readonly string? tempDirectory;

    public IReadMusicBrainzDumpPort Port { get; }

    public IReadOnlyList<string> RecordingDumpPaths { get; }

    public IReadOnlyList<string> ReleaseDumpPaths { get; }

    public static MusicBrainzDumpReaderTestEnvironment Create(MusicBrainzDumpReaderMode mode) =>
        mode switch
        {
            MusicBrainzDumpReaderMode.InProcessFake => CreateFake(),
            MusicBrainzDumpReaderMode.FileSystemJson => CreateFileSystem(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public async Task<IReadOnlyList<MusicBrainzCatalogSeedRecord>> ReadAsync()
    {
        var results = new List<MusicBrainzCatalogSeedRecord>();

        await foreach (var record in Port.ReadAsync(RecordingDumpPaths, ReleaseDumpPaths, CancellationToken.None))
        {
            results.Add(record);
        }

        return results;
    }

    public void Dispose()
    {
        if (!string.IsNullOrWhiteSpace(tempDirectory) && Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static MusicBrainzDumpReaderTestEnvironment CreateFake() =>
        new(
            new FakeMusicBrainzDumpReader(),
            ["recordings.ndjson"],
            ["releases.ndjson"],
            null);

    private static MusicBrainzDumpReaderTestEnvironment CreateFileSystem()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"soundtrail-musicbrainz-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        var recordingPath = Path.Combine(tempDirectory, "recordings.ndjson");
        var releasePath = Path.Combine(tempDirectory, "releases.ndjson");

        File.WriteAllText(recordingPath, RecordingLine + Environment.NewLine);
        File.WriteAllText(releasePath, ReleaseLine + Environment.NewLine);

        return new MusicBrainzDumpReaderTestEnvironment(
            new FileSystemMusicBrainzDumpReader(),
            [recordingPath],
            [releasePath],
            tempDirectory);
    }

    private sealed class FakeMusicBrainzDumpReader : IReadMusicBrainzDumpPort
    {
        public async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
            IReadOnlyList<string> recordingDumpPaths,
            IReadOnlyList<string> releaseDumpPaths,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new MusicBrainzCatalogSeedRecord(
                "recording:mb-recording-1",
                "mb-recording-1",
                "Standalone Song",
                "Standalone Artist",
                "mb-artist-standalone",
                "Standalone Album",
                "mb-release-standalone",
                "USRC17607839",
                "mb-recording-1",
                180000,
                new DateOnly(1976, 1, 1));
            await Task.Yield();

            yield return new MusicBrainzCatalogSeedRecord(
                "release:mb-release-1:medium:1:track:track-1",
                "mb-recording-2",
                "Release Song",
                "Release Artist",
                "mb-artist-release",
                "Release Album",
                "mb-release-1",
                "USIR20400274",
                "mb-recording-2",
                222000,
                new DateOnly(2004, 6, 7));
            await Task.Yield();
        }
    }

    private const string RecordingLine =
        """
        {"id":"mb-recording-1","title":"Standalone Song","length":180000,"isrcs":["USRC17607839"],"artist-credit":[{"name":"Standalone Artist","artist":{"id":"mb-artist-standalone","name":"Standalone Artist"}}],"releases":[{"id":"mb-release-standalone","title":"Standalone Album","date":"1976"}]}
        """;

    private const string ReleaseLine =
        """
        {"id":"mb-release-1","title":"Release Album","date":"2004-06-07","artist-credit":[{"name":"Release Artist","artist":{"id":"mb-artist-release","name":"Release Artist"}}],"media":[{"tracks":[{"id":"track-1","title":"Release Song","length":222000,"recording":{"id":"mb-recording-2","title":"Release Song","length":222000,"isrcs":["USIR20400274"],"artist-credit":[{"name":"Release Artist","artist":{"id":"mb-artist-release","name":"Release Artist"}}]}}]}]}
        """;
}
