using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAlbumMetadataLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnArtistMetadataLookupAttempted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumDiscoveryCompleted.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistDiscoveryCompleted.Support;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class KnownItemDiscoveryCompletionTestEnvironment
{
    private KnownItemDiscoveryCompletionTestEnvironment()
    {
        DiscoveryRepository = new CatalogSearchDiscoveryRepositoryFake();
        ArtistCatalogRepository = new ArtistCatalogEventRepositoryFake();
        ArtistAttemptedHandler = new ArtistMetadataLookupAttemptedHandler(DiscoveryRepository);
        AlbumAttemptedHandler = new AlbumMetadataLookupAttemptedHandler(DiscoveryRepository);
        ApplyArtistDiscoveryCompletedHandler = new ApplyKnownArtistDiscoveryCompletedToArtistCatalogHandler(ArtistCatalogRepository);
        ApplyAlbumDiscoveryCompletedHandler = new ApplyKnownAlbumDiscoveryCompletedToArtistCatalogHandler(ArtistCatalogRepository);
    }

    public CatalogSearchDiscoveryRepositoryFake DiscoveryRepository { get; }

    public ArtistCatalogEventRepositoryFake ArtistCatalogRepository { get; }

    public ArtistMetadataLookupAttemptedHandler ArtistAttemptedHandler { get; }

    public AlbumMetadataLookupAttemptedHandler AlbumAttemptedHandler { get; }

    public ApplyKnownArtistDiscoveryCompletedToArtistCatalogHandler ApplyArtistDiscoveryCompletedHandler { get; }

    public ApplyKnownAlbumDiscoveryCompletedToArtistCatalogHandler ApplyAlbumDiscoveryCompletedHandler { get; }

    public static KnownItemDiscoveryCompletionTestEnvironment Create() => new();

    public async Task SeedKnownArtistAsync(string artistId = "artist_1")
    {
        var knownItem = KnownCatalogItem.ForArtist(ArtistId.From(artistId));
        var loaded = await global::Soundtrail.Domain.Discovery.KnownItemDiscovery.LoadAsync(DiscoveryRepository, knownItem, CancellationToken.None);
        loaded.Aggregate.ArtistRequested(ArtistId.From(artistId), Clock, CorrelationId.From("corr-artist"));
        await loaded.Aggregate.SaveAsync(DiscoveryRepository, loaded.Stream, CancellationToken.None);
    }

    public async Task SeedKnownAlbumAsync(string artistId = "artist_1", string albumId = "album_1")
    {
        var knownItem = KnownCatalogItem.ForAlbum(ArtistId.From(artistId), AlbumId.From(albumId));
        var loaded = await global::Soundtrail.Domain.Discovery.KnownItemDiscovery.LoadAsync(DiscoveryRepository, knownItem, CancellationToken.None);
        loaded.Aggregate.AlbumRequested(ArtistId.From(artistId), AlbumId.From(albumId), Clock, CorrelationId.From("corr-album"));
        await loaded.Aggregate.SaveAsync(DiscoveryRepository, loaded.Stream, CancellationToken.None);
    }

    public ArtistMetadataLookupAttempted CompletedArtistAttempted() =>
        ArtistMetadataLookupAttempted.Completed(
            new ArtistMetadataFetched(
                CommandId.For("LookupArtistMetadata:artist_1"),
                ArtistId.From("artist_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                Clock,
                new ArtistMetadata("The Killers", "mb-artist-the-killers"),
                CorrelationId.From("corr-artist")));

    public AlbumMetadataLookupAttempted CompletedAlbumAttempted() =>
        AlbumMetadataLookupAttempted.Completed(
            new AlbumMetadataFetched(
                CommandId.For("LookupAlbumMetadata:artist_1:album_1"),
                ArtistId.From("artist_1"),
                AlbumId.From("album_1"),
                LookupSource.MusicBrainz,
                LookupPriorityBand.High,
                Clock,
                new AlbumMetadata("Hot Fuss", "The Killers", "mb-release-hot-fuss", "mb-artist-the-killers", new DateOnly(2004, 6, 7)),
                CorrelationId.From("corr-album")));

    public KnownArtistDiscoveryCompletedCommand ArtistCompletedCommand()
    {
        var knownItem = KnownCatalogItem.ForArtist(ArtistId.From("artist_1"));
        return new KnownArtistDiscoveryCompletedCommand(
            DiscoveryQueryKey.StableValueFor(knownItem),
            [new VersionedCatalogSearchDiscoveryEvent(2, new KnownArtistDiscoveryCompleted(
                ArtistId.From("artist_1"),
                LookupPriorityBand.High,
                LookupSource.MusicBrainz,
                "Discovery completed",
                Clock,
                "The Killers",
                "mb-artist-the-killers"))]);
    }

    public KnownAlbumDiscoveryCompletedCommand AlbumCompletedCommand()
    {
        var knownItem = KnownCatalogItem.ForAlbum(ArtistId.From("artist_1"), AlbumId.From("album_1"));
        return new KnownAlbumDiscoveryCompletedCommand(
            DiscoveryQueryKey.StableValueFor(knownItem),
            [new VersionedCatalogSearchDiscoveryEvent(2, new KnownAlbumDiscoveryCompleted(
                ArtistId.From("artist_1"),
                AlbumId.From("album_1"),
                LookupPriorityBand.High,
                LookupSource.MusicBrainz,
                "Discovery completed",
                Clock,
                "Hot Fuss",
                "The Killers",
                "mb-release-hot-fuss",
                "mb-artist-the-killers",
                new DateOnly(2004, 6, 7)))]);
    }

    private static readonly DateTimeOffset Clock = new(2026, 6, 29, 12, 0, 0, TimeSpan.Zero);
}
