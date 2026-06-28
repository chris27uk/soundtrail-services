using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Support;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicTrackStreamStore;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedResponsesTests
{
    [Fact]
    public async Task Given_A_New_Stream_When_Appending_Facts_Then_They_Can_Be_Loaded_Back()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        using var session = raven.Store.OpenAsyncSession();
        var store = CreateStore(session);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        var append = await AppendAsync(
            store,
            musicCatalogId,
            expectedVersion: 0,
            CommandId.For("ResolveMusicMetadata:mc_track_1"),
            [
                new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))
            ]);
        await session.SaveChangesAsync(CancellationToken.None);

        append.Appended.Should().BeTrue();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await LoadAsync(verificationStore, musicCatalogId);

        loaded.Version.Should().Be(1);
        loaded.Events.Should().ContainItemsAssignableTo<TrackDiscovered>();
        var fact = loaded.Events.Should().ContainSingle().Subject;
        fact.Should().BeOfType<TrackDiscovered>();
        ((TrackDiscovered)fact).Mbid.Should().Be("mbid-1");
    }

    [Fact]
    public async Task Given_A_New_Stream_When_Appending_Then_Separate_Metadata_And_Event_Documents_Are_Persisted()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        using (var session = raven.Store.OpenAsyncSession())
        {
            var store = CreateStore(session);
            await AppendAsync(
                store,
                musicCatalogId,
                expectedVersion: 0,
                CommandId.For("ResolveMusicMetadata:mc_track_1"),
                [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))]);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var metadata = await verificationSession.LoadAsync<RavenEventStreamMetadataRecord>(
            $"music-track-streams/{musicCatalogId.Value}",
            CancellationToken.None);
        var storedEvent = await verificationSession.LoadAsync<RavenStoredEventRecord>(
            $"music-track-events/{musicCatalogId.Value}/0000000001",
            CancellationToken.None);

        metadata.Should().NotBeNull();
        metadata!.Version.Should().Be(1);
        storedEvent.Should().NotBeNull();
        storedEvent!.Version.Should().Be(1);
        storedEvent.EventType.Should().Be(nameof(TrackDiscovered));
        storedEvent.Body.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_The_Same_Command_Id_When_Appending_Twice_Then_The_Second_Append_Is_Ignored()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var commandId = CommandId.For("ResolveMusicMetadata:mc_track_1");

        using (var firstSession = raven.Store.OpenAsyncSession())
        {
            var store = CreateStore(firstSession);
            await AppendAsync(
                store,
                musicCatalogId,
                expectedVersion: 0,
                commandId,
                [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))]);
            await firstSession.SaveChangesAsync(CancellationToken.None);
        }

        using var secondSession = raven.Store.OpenAsyncSession();
        var secondStore = CreateStore(secondSession);
        var duplicate = await AppendAsync(
            secondStore,
            musicCatalogId,
            expectedVersion: 1,
            commandId,
            [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))]);
        await secondSession.SaveChangesAsync(CancellationToken.None);

        duplicate.Appended.Should().BeFalse();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await LoadAsync(verificationStore, musicCatalogId);
        loaded.Version.Should().Be(1);
        loaded.Events.Should().ContainSingle();
    }

    private static IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> CreateStore(IAsyncDocumentSession session) =>
        TestEventStreamRepositories.CreateMusicTrack(session);

    private static Task<AppendResult<IMusicTrackEvent>> AppendAsync(
        IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> store,
        MusicCatalogId musicCatalogId,
        int expectedVersion,
        CommandId commandId,
        IReadOnlyList<IMusicTrackEvent> events) =>
        store.AppendAsync(
            new AppendRequest<MusicCatalogId, IMusicTrackEvent>(
                musicCatalogId,
                expectedVersion,
                events,
                OperationId.From(commandId.Value)),
            CancellationToken.None);

    private static async Task<MusicTrackStream> LoadAsync(
        IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> store,
        MusicCatalogId musicCatalogId)
    {
        var stream = await store.LoadAsync(musicCatalogId, CancellationToken.None);
        return new MusicTrackStream(stream.Version, stream.Events);
    }
}
