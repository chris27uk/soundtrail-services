using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.Domain.Discovery;

public sealed class KnownItemDiscoveryTests
{
    [Fact]
    public async Task Given_An_Empty_Known_Item_Stream_When_Requesting_An_Artist_Then_Artist_Catalog_Lookup_Requested_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var knownItem = KnownCatalogItem.ForArtist(ArtistId.From("artist_1"));
        var loaded = await KnownItemDiscovery.LoadAsync(repository, knownItem, CancellationToken.None);

        var changed = loaded.Aggregate.ArtistRequested(
            ArtistId.From("artist_1"),
            Clock,
            CorrelationId.From("corr-1"));
        await loaded.Aggregate.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        changed.Should().BeTrue();
        repository.GetStoredEvents(knownItem).Should().ContainSingle().Which.Should().BeOfType<ArtistCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_An_Empty_Known_Item_Stream_When_Requesting_An_Album_Then_Album_Catalog_Lookup_Requested_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var knownItem = KnownCatalogItem.ForAlbum(AlbumId.From("album_1"));
        var loaded = await KnownItemDiscovery.LoadAsync(repository, knownItem, CancellationToken.None);

        var changed = loaded.Aggregate.AlbumRequested(
            null,
            AlbumId.From("album_1"),
            Clock,
            CorrelationId.From("corr-1"));
        await loaded.Aggregate.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        changed.Should().BeTrue();
        repository.GetStoredEvents(knownItem).Should().ContainSingle().Which.Should().BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_An_Empty_Known_Item_Stream_When_Requesting_A_Track_Then_Known_Track_Requested_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var knownItem = KnownCatalogItem.ForTrack(TrackId.From("track_1"));
        var loaded = await KnownItemDiscovery.LoadAsync(repository, knownItem, CancellationToken.None);

        var changed = loaded.Aggregate.TrackRequested(
            TrackId.From("track_1"),
            PlaybackProviderFilter.Parse("spotify"),
            Clock,
            CorrelationId.From("corr-1"));
        await loaded.Aggregate.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        changed.Should().BeTrue();
        repository.GetStoredEvents(knownItem).Should().ContainSingle().Which.Should().BeOfType<KnownTrackRequestedEvent>();
    }

    [Fact]
    public async Task Given_An_Already_Requested_Known_Artist_When_Requesting_Again_Then_No_New_Event_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var knownItem = KnownCatalogItem.ForArtist(ArtistId.From("artist_1"));
        var loaded = await KnownItemDiscovery.LoadAsync(repository, knownItem, CancellationToken.None);
        loaded.Aggregate.ArtistRequested(ArtistId.From("artist_1"), Clock, CorrelationId.From("corr-1"));
        await loaded.Aggregate.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        loaded = await KnownItemDiscovery.LoadAsync(repository, knownItem, CancellationToken.None);
        var changed = loaded.Aggregate.ArtistRequested(ArtistId.From("artist_1"), Clock, CorrelationId.From("corr-1"));

        changed.Should().BeFalse();
        repository.GetStoredEvents(knownItem).Should().ContainSingle();
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
}
