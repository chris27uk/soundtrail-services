using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicTrackStreamStore;

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
                new MinimalTrackInfoDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))
            ],
            CancellationToken.None);

        append.Appended.Should().BeTrue();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await verificationStore.LoadEventsAsync(musicCatalogId, CancellationToken.None);

        loaded.Version.Should().Be(1);
        loaded.Facts.Should().ContainItemsAssignableTo<MinimalTrackInfoDiscovered>();
        var fact = loaded.Facts.Should().ContainSingle().Subject;
        fact.Should().BeOfType<MinimalTrackInfoDiscovered>();
        ((MinimalTrackInfoDiscovered)fact).Mbid.Should().Be("mbid-1");
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
                [new MinimalTrackInfoDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))],
                CancellationToken.None);
        }

        using var secondSession = raven.Store.OpenAsyncSession();
        var secondStore = CreateStore(secondSession);
        var duplicate = await secondStore.AppendEventsAsync(
            musicCatalogId,
            expectedVersion: 1,
            commandId,
            [new MinimalTrackInfoDiscovered("Song A", "Artist A", 123000, "isrc-1", "mbid-1", ProviderName.MusicBrainz, new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))],
            CancellationToken.None);

        duplicate.Appended.Should().BeFalse();

        using var verificationSession = raven.Store.OpenAsyncSession();
        var verificationStore = CreateStore(verificationSession);
        var loaded = await verificationStore.LoadEventsAsync(musicCatalogId, CancellationToken.None);
        loaded.Version.Should().Be(1);
        loaded.Facts.Should().ContainSingle();
    }

    private static IMusicTrackEventRepository CreateStore(IAsyncDocumentSession session) =>
        new RavenMusicTrackStreamStore(session);
}
