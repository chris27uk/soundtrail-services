using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;

namespace Soundtrail.Services.Tests.Unit.MusicBrainzImport.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandlerTests
{
    [Fact]
    public async Task Given_A_Release_Record_When_Imported_Then_Catalog_Events_Are_Appended_And_Projected()
    {
        var env = ImportMusicBrainzDumpHandlerTestEnvironment.Create([
            new MusicBrainzCatalogSeedRecord(
                "release:mb-release-1:medium:1:track:mb-recording-1",
                "mb-recording-1",
                "Mr. Brightside",
                "The Killers",
                "mb-artist-1",
                "Hot Fuss",
                "mb-release-1",
                "USIR20400274",
                "mb-recording-1",
                222000,
                new DateOnly(2004, 6, 7))
        ]);

        var result = await env.Handler.Handle(
            new ImportMusicBrainzDumpCommand([], [], true, Clock),
            CancellationToken.None);

        result.ProcessedRecordCount.Should().Be(1);
        result.ImportedRecordCount.Should().Be(1);
        result.ProjectedRecordCount.Should().Be(1);
        result.SkippedRecordCount.Should().Be(0);

        var stream = await env.StreamStore.LoadEventsAsync(MusicCatalogId.From("mc_track_mbrecording1"), CancellationToken.None);
        stream.Events.Should().ContainItemsAssignableTo<TrackDiscovered>();
        stream.Events.Should().ContainItemsAssignableTo<ArtistDiscovered>();
        stream.Events.Should().ContainItemsAssignableTo<AlbumDiscovered>();
        stream.Events.Should().ContainSingle(x => x is PlaybackReferencesResolutionRequired);

        var projection = env.ProjectionStore.Load(MusicCatalogId.From("mc_track_mbrecording1"));
        projection.Should().NotBeNull();
        projection!.Track.Title.Should().Be("Mr. Brightside");
        projection.Track.ArtistId.Should().Be("artist_mbartist1");
        projection.Track.AlbumId.Should().Be("album_mbrelease1");
        projection.Track.MusicBrainzRecordingId.Should().Be("mb-recording-1");
    }

    [Fact]
    public async Task Given_The_Same_Record_When_Imported_Twice_Then_The_Second_Run_Is_Idempotent()
    {
        var record = new MusicBrainzCatalogSeedRecord(
            "recording:mb-recording-1",
            "mb-recording-1",
            "Song A",
            "Artist A",
            "mb-artist-1",
            null,
            null,
            "isrc-1",
            "mb-recording-1",
            123000,
            null);
        var env = ImportMusicBrainzDumpHandlerTestEnvironment.Create([record]);
        var command = new ImportMusicBrainzDumpCommand([], [], false, Clock);

        var first = await env.Handler.Handle(command, CancellationToken.None);
        var second = await env.Handler.Handle(command, CancellationToken.None);

        first.ImportedRecordCount.Should().Be(1);
        second.ImportedRecordCount.Should().Be(0);
        second.SkippedRecordCount.Should().Be(1);

        var stream = await env.StreamStore.LoadEventsAsync(MusicCatalogId.From("mc_track_mbrecording1"), CancellationToken.None);
        stream.Events.Should().ContainSingle(x => x is TrackDiscovered);
        stream.Events.Should().ContainSingle(x => x is ArtistDiscovered);
        stream.Events.Should().ContainSingle(x => x is PlaybackReferencesResolutionRequired);
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private sealed class ImportMusicBrainzDumpHandlerTestEnvironment
    {
        private ImportMusicBrainzDumpHandlerTestEnvironment(
            ImportMusicBrainzDumpHandler handler,
            MusicTrackStreamStoreFake streamStore,
            ProjectionStoreFake projectionStore)
        {
            Handler = handler;
            StreamStore = streamStore;
            ProjectionStore = projectionStore;
        }

        public ImportMusicBrainzDumpHandler Handler { get; }

        public MusicTrackStreamStoreFake StreamStore { get; }

        public ProjectionStoreFake ProjectionStore { get; }

        public static ImportMusicBrainzDumpHandlerTestEnvironment Create(
            IReadOnlyList<MusicBrainzCatalogSeedRecord> records)
        {
            var streamStore = new MusicTrackStreamStoreFake();
            var projectionStore = new ProjectionStoreFake();
            var projectionHandler = new ProjectMusicTrackCatalogHandler(projectionStore, projectionStore);
            var handler = new ImportMusicBrainzDumpHandler(
                new FakeMusicBrainzDumpReader(records),
                streamStore,
                projectionHandler);

            return new ImportMusicBrainzDumpHandlerTestEnvironment(handler, streamStore, projectionStore);
        }
    }

    private sealed class FakeMusicBrainzDumpReader(
        IReadOnlyList<MusicBrainzCatalogSeedRecord> records) : IReadMusicBrainzDumpPort
    {
        public async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
            IReadOnlyList<string> recordingDumpPaths,
            IReadOnlyList<string> releaseDumpPaths,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return record;
                await Task.Yield();
            }
        }
    }

    private sealed class ProjectionStoreFake :
        ILoadMusicTrackCatalogProjectionPort,
        ISaveMusicTrackCatalogProjectionPort
    {
        private readonly Dictionary<string, MusicTrackCatalogProjectionSnapshot> projections = new(StringComparer.Ordinal);

        public MusicTrackCatalogProjection? Load(MusicCatalogId musicCatalogId) =>
            projections.TryGetValue(musicCatalogId.Value, out var snapshot)
                ? MusicTrackCatalogProjection.Load(snapshot)
                : null;

        public Task<MusicTrackCatalogProjection> LoadAsync(
            MusicCatalogId musicCatalogId,
            CancellationToken cancellationToken)
        {
            var projection = Load(musicCatalogId)
                ?? MusicTrackCatalogProjection.Load(
                    new MusicTrackCatalogProjectionSnapshot(
                        musicCatalogId,
                        new CatalogTrackProjection(
                            musicCatalogId.Value,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            null,
                            null,
                            null,
                            [],
                            [],
                            [],
                            null,
                            default),
                        null,
                        null,
                        0));
            return Task.FromResult(projection);
        }

        public Task SaveAsync(
            MusicTrackCatalogProjection projection,
            CancellationToken cancellationToken)
        {
            projections[projection.MusicCatalogId.Value] = projection.ToSnapshot();
            return Task.CompletedTask;
        }
    }
}
