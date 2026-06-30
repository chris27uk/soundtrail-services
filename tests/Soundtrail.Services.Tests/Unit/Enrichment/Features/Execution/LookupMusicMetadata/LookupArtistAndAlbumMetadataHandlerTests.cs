using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.LookupMusicMetadata;

public sealed class LookupArtistAndAlbumMetadataHandlerTests
{
    [Fact]
    public async Task Given_An_Artist_Metadata_Command_When_Metadata_Is_Returned_Then_A_Completed_Attempt_Is_Sent()
    {
        var bus = new CommandBusFake();
        var handler = new LookupArtistMetadataHandler(
            new ArtistMetadataSourceFake(new ArtistMetadata("The Killers", "mb-artist-1")),
            bus);

        await handler.Handle(
            new LookupArtistMetadataCommand(
                CommandId.For("LookupArtistMetadata:artist_1"),
                ArtistId.From("artist_1"),
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
                CorrelationId.From("corr-artist"),
                "The Killers",
                "mb-artist-1"),
            CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<ArtistMetadataLookupAttempted>()
            .Which.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Completed);
    }

    [Fact]
    public async Task Given_An_Album_Metadata_Command_When_Metadata_Is_Returned_Then_A_Completed_Attempt_Is_Sent()
    {
        var bus = new CommandBusFake();
        var handler = new LookupAlbumMetadataHandler(
            new AlbumMetadataSourceFake(new AlbumMetadata("Hot Fuss", "The Killers", "mb-release-1", "mb-artist-1", new DateOnly(2004, 6, 7))),
            bus);

        await handler.Handle(
            new LookupAlbumMetadataCommand(
                CommandId.For("LookupAlbumMetadata:artist_1:album_1"),
                ArtistId.From("artist_1"),
                AlbumId.From("album_1"),
                LookupPriorityBand.High,
                new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero),
                CorrelationId.From("corr-album"),
                "The Killers",
                "Hot Fuss",
                "mb-release-1",
                "mb-artist-1"),
            CancellationToken.None);

        bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<AlbumMetadataLookupAttempted>()
            .Which.Outcome.Status.Should().Be(MusicCatalogLookupOutcomeStatus.Completed);
    }

    private sealed class ArtistMetadataSourceFake(ArtistMetadata metadata) : IGetArtistMetadata
    {
        public Task<ArtistMetadata?> GetMetadataAsync(string artistName, string? sourceArtistId, CancellationToken cancellationToken) =>
            Task.FromResult<ArtistMetadata?>(metadata);
    }

    private sealed class AlbumMetadataSourceFake(AlbumMetadata metadata) : IGetAlbumMetadata
    {
        public Task<AlbumMetadata?> GetMetadataAsync(
            string artistName,
            string albumTitle,
            string? sourceAlbumId,
            string? sourceArtistId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AlbumMetadata?>(metadata);
    }
}
