using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicTrackStreamStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class MusicTrackStreamStoreContractResponsesTests
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_New_Stream_When_Appending_Then_Events_Can_Be_Loaded_Back(StreamStoreMode mode)
    {
        await using var env = StreamStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await env.AppendAsync(
            musicCatalogId,
            0,
            CommandId.For("LookupCanonicalMusicMetadata:mc_track_1"),
            [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))]);

        var loaded = await env.LoadAsync(musicCatalogId);

        loaded.Version.Should().Be(1);
        loaded.Events.Should().ContainSingle().Which.Should().BeOfType<TrackDiscovered>();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_The_Same_CommandId_When_Appending_Twice_Then_The_Second_Append_Is_Ignored(StreamStoreMode mode)
    {
        await using var env = StreamStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var commandId = CommandId.For("LookupCanonicalMusicMetadata:mc_track_1");
        var @event = new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero));

        var first = await env.AppendAsync(musicCatalogId, 0, commandId, [@event]);
        var second = await env.AppendAsync(musicCatalogId, 1, commandId, [@event]);

        first.Appended.Should().BeTrue();
        second.Appended.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_New_Spec_Events_When_Appending_Then_They_Can_Be_Loaded_Back(StreamStoreMode mode)
    {
        await using var env = StreamStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await env.AppendAsync(
            musicCatalogId,
            0,
            CommandId.For("RepairMetadata:mc_track_1"),
            [
                new ArtworkDiscovered(
                    Domain.Catalog.CatalogEntityKind.Track,
                    null,
                    new Uri("https://images.example.com/track.png"),
                    "worker/musicbrainz",
                    new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                new MetadataCorrected(
                    "Song A (Remastered)",
                    "Artist A",
                    "artist_a",
                    "mb-artist-a",
                    "Album A",
                    "album_a",
                    "mb-release-a",
                    new DateOnly(2004, 6, 7),
                    123000,
                    "isrc-1",
                    "mbid-1",
                    "admin/repair",
                    new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero))
            ]);

        var loaded = await env.LoadAsync(musicCatalogId);

        loaded.Version.Should().Be(2);
        loaded.Events[0].Should().BeOfType<ArtworkDiscovered>();
        loaded.Events[1].Should().BeOfType<MetadataCorrected>();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Metadata_Response_With_References_When_Recorded_Through_The_Aggregate_Then_No_Playback_Follow_Up_Is_Stored(StreamStoreMode mode)
    {
        await using var env = StreamStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        await env.RecordAsync(
            musicCatalogId,
            MusicBrainzResponse() with
            {
                References =
                [
                    new ExternalReference(
                        ProviderName.Spotify,
                        new Uri("https://open.spotify.com/track/spotify-1"),
                        "spotify-1")
                ]
            });

        var loaded = await env.LoadAsync(musicCatalogId);

        loaded.Events.Should().Contain(x => x is ProviderReferenceDiscovered);
        loaded.Events.Should().NotContain(x => x is StreamingLocationsRequired);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Given_A_Rehydrated_Aggregate_When_Metadata_Is_Recorded_Again_Then_Playback_Follow_Up_Is_Not_Duplicated(StreamStoreMode mode)
    {
        await using var env = StreamStoreTestEnvironment.Create(mode);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await env.RecordAsync(
            musicCatalogId,
            MusicBrainzResponse() with
            {
                CommandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1:first")
            });
        await env.RecordAsync(
            musicCatalogId,
            MusicBrainzResponse() with
            {
                CommandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1:second"),
                CreatedAt = new DateTimeOffset(2026, 6, 6, 12, 5, 0, TimeSpan.Zero)
            });

        var loaded = await env.LoadAsync(musicCatalogId);

        loaded.Events.OfType<StreamingLocationsRequired>().Should().ContainSingle();
    }

    public static IEnumerable<object[]> AllModes()
    {
        yield return [StreamStoreMode.InProcessFake];
        yield return [StreamStoreMode.RavenEmbedded];
    }

    public enum StreamStoreMode
    {
        InProcessFake,
        RavenEmbedded
    }

    private sealed class StreamStoreTestEnvironment : IAsyncDisposable
    {
        private readonly MusicTrackStreamStoreFake? fake;
        private readonly RavenEmbeddedTestDatabase? raven;

        private StreamStoreTestEnvironment(MusicTrackStreamStoreFake? fake, RavenEmbeddedTestDatabase? raven)
        {
            this.fake = fake;
            this.raven = raven;
        }

        public static StreamStoreTestEnvironment Create(StreamStoreMode mode) =>
            mode switch
            {
                StreamStoreMode.InProcessFake => new(new MusicTrackStreamStoreFake(), null),
                StreamStoreMode.RavenEmbedded => new(null, RavenEmbeddedTestDatabase.Create()),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

        public async Task RecordAsync(MusicCatalogId musicCatalogId, MusicCatalogMetadataFetched response)
        {
            if (fake is not null)
            {
                var aggregate = await CatalogEntityAggregate.LoadAsync(fake, musicCatalogId, CancellationToken.None);
                aggregate.RecordMusicCatalogMetadataFetched(response);
                await aggregate.SaveAsync(fake, response.CommandId, CancellationToken.None);
                return;
            }

            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenMusicTrackStreamStore(session);
            var ravenAggregate = await CatalogEntityAggregate.LoadAsync(store, musicCatalogId, CancellationToken.None);
            ravenAggregate.RecordMusicCatalogMetadataFetched(response);
            await ravenAggregate.SaveAsync(store, response.CommandId, CancellationToken.None);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        public Task<AppendMusicTrackStreamResult> AppendAsync(MusicCatalogId musicCatalogId, int expectedVersion, CommandId commandId, IReadOnlyList<IMusicTrackEvent> events)
        {
            if (fake is not null)
            {
                return fake.AppendEventsAsync(musicCatalogId, expectedVersion, commandId, events, CancellationToken.None);
            }

            return AppendRavenAsync(musicCatalogId, expectedVersion, commandId, events);
        }

        private async Task<AppendMusicTrackStreamResult> AppendRavenAsync(MusicCatalogId musicCatalogId, int expectedVersion, CommandId commandId, IReadOnlyList<IMusicTrackEvent> events)
        {
            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenMusicTrackStreamStore(session);
            var append = await store.AppendEventsAsync(musicCatalogId, expectedVersion, commandId, events, CancellationToken.None);
            await session.SaveChangesAsync(CancellationToken.None);
            return append;
        }

        public Task<MusicTrackStream> LoadAsync(MusicCatalogId musicCatalogId)
        {
            if (fake is not null)
            {
                return fake.LoadEventsAsync(musicCatalogId, CancellationToken.None);
            }

            return LoadRavenAsync(musicCatalogId);
        }

        private async Task<MusicTrackStream> LoadRavenAsync(MusicCatalogId musicCatalogId)
        {
            using var session = raven!.Store.OpenAsyncSession();
            var store = new RavenMusicTrackStreamStore(session);
            return await store.LoadEventsAsync(musicCatalogId, CancellationToken.None);
        }

        public ValueTask DisposeAsync()
        {
            raven?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private static MusicCatalogMetadataFetched MusicBrainzResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000, "Album A", new DateOnly(2004, 6, 7), "mb-artist-1", "mb-release-1"),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_hot_fuss")),
            CorrelationId.From("corr-1"));
}
