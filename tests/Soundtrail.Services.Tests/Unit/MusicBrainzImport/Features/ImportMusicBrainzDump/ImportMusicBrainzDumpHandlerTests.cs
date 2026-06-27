using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;
using System.Runtime.CompilerServices;

namespace Soundtrail.Services.Tests.Unit.MusicBrainzImport.Features.ImportMusicBrainzDump;

public sealed class ImportMusicBrainzDumpHandlerTests
{
    [Fact]
    public async Task Given_A_Release_Record_When_Imported_Then_Catalog_Events_Are_Appended()
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

        await env.Handler.Handle(
            new ImportMusicBrainzDumpCommand([], [], Clock),
            CancellationToken.None);

        var stream = await env.StreamStore.LoadEventsAsync(MusicCatalogId.From("mc_track_mbrecording1"), CancellationToken.None);
        stream.Events.Should().ContainItemsAssignableTo<TrackDiscovered>();
        stream.Events.Should().ContainItemsAssignableTo<ArtistDiscovered>();
        stream.Events.Should().ContainItemsAssignableTo<AlbumDiscovered>();
        stream.Events.Should().ContainSingle(x => x is StreamingLocationsRequired);
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
        var command = new ImportMusicBrainzDumpCommand([], [], Clock);

        await env.Handler.Handle(command, CancellationToken.None);
        await env.Handler.Handle(command, CancellationToken.None);

        var stream = await env.StreamStore.LoadEventsAsync(MusicCatalogId.From("mc_track_mbrecording1"), CancellationToken.None);
        stream.Events.Should().ContainSingle(x => x is TrackDiscovered);
        stream.Events.Should().ContainSingle(x => x is ArtistDiscovered);
        stream.Events.Should().ContainSingle(x => x is StreamingLocationsRequired);
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private sealed class ImportMusicBrainzDumpHandlerTestEnvironment
    {
        private ImportMusicBrainzDumpHandlerTestEnvironment(
            ImportMusicBrainzDumpHandler handler,
            MusicTrackStreamStoreFake streamStore)
        {
            Handler = handler;
            StreamStore = streamStore;
        }

        public ImportMusicBrainzDumpHandler Handler { get; }

        public MusicTrackStreamStoreFake StreamStore { get; }

        public static ImportMusicBrainzDumpHandlerTestEnvironment Create(
            IReadOnlyList<MusicBrainzCatalogSeedRecord> records)
        {
            var streamStore = new MusicTrackStreamStoreFake();
            var handler = new ImportMusicBrainzDumpHandler(
                new FakeMusicBrainzDumpReader(records),
                streamStore);

            return new ImportMusicBrainzDumpHandlerTestEnvironment(handler, streamStore);
        }
    }

    private sealed class FakeMusicBrainzDumpReader(
        IReadOnlyList<MusicBrainzCatalogSeedRecord> records) : IReadMusicBrainzDumpPort
    {
        public async IAsyncEnumerable<MusicBrainzCatalogSeedRecord> ReadAsync(
            IReadOnlyList<string> recordingDumpPaths,
            IReadOnlyList<string> releaseDumpPaths,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return record;
                await Task.Yield();
            }
        }
    }

}
