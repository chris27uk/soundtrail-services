using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

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

        var append = await store.AppendEventsAsync(
            musicCatalogId,
            expectedVersion: 0,
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            [
                new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))
            ],
            CancellationToken.None);
        await session.SaveChangesAsync(CancellationToken.None);

        append.Appended.Should().BeTrue();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await verificationStore.LoadEventsAsync(musicCatalogId, CancellationToken.None);

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
            await store.AppendEventsAsync(
                musicCatalogId,
                expectedVersion: 0,
                CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
                [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))],
                CancellationToken.None);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var metadata = await verificationSession.LoadAsync<MusicTrackEventStreamMetadataRecordDto>(
            MusicTrackEventStreamMetadataRecordDto.GetDocumentId(musicCatalogId.Value),
            CancellationToken.None);
        var storedEvent = await verificationSession.LoadAsync<MusicTrackStoredEventRecordDto>(
            MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 1),
            CancellationToken.None);

        metadata.Should().NotBeNull();
        metadata!.Version.Should().Be(1);
        storedEvent.Should().NotBeNull();
        storedEvent!.Version.Should().Be(1);
        storedEvent.EventType.Should().Be(nameof(TrackDiscovered));
    }

    [Fact]
    public async Task Given_The_Same_Command_Id_When_Appending_Twice_Then_The_Second_Append_Is_Ignored()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var commandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1");

        using (var firstSession = raven.Store.OpenAsyncSession())
        {
            var store = CreateStore(firstSession);
            await store.AppendEventsAsync(
                musicCatalogId,
                expectedVersion: 0,
                commandId,
                [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))],
                CancellationToken.None);
            await firstSession.SaveChangesAsync(CancellationToken.None);
        }

        using var secondSession = raven.Store.OpenAsyncSession();
        var secondStore = CreateStore(secondSession);
        var duplicate = await secondStore.AppendEventsAsync(
            musicCatalogId,
            expectedVersion: 1,
            commandId,
            [new TrackDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))],
            CancellationToken.None);
        await secondSession.SaveChangesAsync(CancellationToken.None);

        duplicate.Appended.Should().BeFalse();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await verificationStore.LoadEventsAsync(musicCatalogId, CancellationToken.None);
        loaded.Version.Should().Be(1);
        loaded.Events.Should().ContainSingle();
    }

    private static IMusicTrackEventRepository CreateStore(IAsyncDocumentSession session) =>
        new RavenMusicTrackStreamStore(session);
}
