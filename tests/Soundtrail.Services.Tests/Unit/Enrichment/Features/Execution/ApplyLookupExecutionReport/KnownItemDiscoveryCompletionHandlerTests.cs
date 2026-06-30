using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyLookupExecutionReport;

public sealed class KnownItemDiscoveryCompletionHandlerTests
{
    [Fact]
    public async Task Given_A_Known_Artist_Lookup_Completion_When_Attempted_Is_Handled_Then_Discovery_Stores_Completed_Facts_With_Catalog_Data()
    {
        var env = KnownItemDiscoveryCompletionTestEnvironment.Create();
        await env.SeedKnownArtistAsync();

        await env.ArtistAttemptedHandler.Handle(env.CompletedArtistAttempted(), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForArtist(ArtistId.From("artist_1")))
            .Last().Should().BeEquivalentTo(new KnownArtistDiscoveryCompleted(
                ArtistId.From("artist_1"),
                LookupPriorityBand.High,
                LookupSource.MusicBrainz,
                "Discovery completed",
                new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
                "The Killers",
                "mb-artist-the-killers"));
    }

    [Fact]
    public async Task Given_A_Known_Album_Lookup_Completion_When_Attempted_Is_Handled_Then_Discovery_Stores_Completed_Facts_With_Catalog_Data()
    {
        var env = KnownItemDiscoveryCompletionTestEnvironment.Create();
        await env.SeedKnownAlbumAsync();

        await env.AlbumAttemptedHandler.Handle(env.CompletedAlbumAttempted(), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(KnownCatalogItem.ForAlbum(ArtistId.From("artist_1"), AlbumId.From("album_1")))
            .Last().Should().BeEquivalentTo(new KnownAlbumDiscoveryCompleted(
                ArtistId.From("artist_1"),
                AlbumId.From("album_1"),
                LookupPriorityBand.High,
                LookupSource.MusicBrainz,
                "Discovery completed",
                new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
                "Hot Fuss",
                "The Killers",
                "mb-release-hot-fuss",
                "mb-artist-the-killers",
                new DateOnly(2004, 6, 7)));
    }

    [Fact]
    public async Task Given_A_Known_Artist_Discovery_Completion_When_Applied_To_Catalog_Then_Artist_Facts_Are_Stored_In_Artist_Stream()
    {
        var env = KnownItemDiscoveryCompletionTestEnvironment.Create();

        await env.ApplyArtistDiscoveryCompletedHandler.Handle(env.ArtistCompletedCommand(), CancellationToken.None);

        env.ArtistCatalogRepository
            .GetStoredEvents(ArtistId.From("artist_1"))
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ArtistDiscovered(
                "artist_1",
                "The Killers",
                "mb-artist-the-killers",
                LookupSource.MusicBrainz,
                new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Given_A_Known_Album_Discovery_Completion_When_Applied_To_Catalog_Then_Artist_And_Album_Facts_Are_Stored_In_Artist_Stream()
    {
        var env = KnownItemDiscoveryCompletionTestEnvironment.Create();

        await env.ApplyAlbumDiscoveryCompletedHandler.Handle(env.AlbumCompletedCommand(), CancellationToken.None);

        env.ArtistCatalogRepository
            .GetStoredEvents(ArtistId.From("artist_1"))
            .Should().Contain(x => x is ArtistDiscovered)
            .And.Contain(x => x is AlbumDiscovered);
    }
}
