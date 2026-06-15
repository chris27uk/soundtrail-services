using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
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
            return await store.AppendEventsAsync(musicCatalogId, expectedVersion, commandId, events, CancellationToken.None);
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
}
