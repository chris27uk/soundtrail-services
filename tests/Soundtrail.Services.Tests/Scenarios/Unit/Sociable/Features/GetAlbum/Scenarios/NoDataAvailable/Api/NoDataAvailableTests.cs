using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbum.Scenarios.NoDataAvailable.Api;

public sealed class NoDataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Album_Is_Returned()
    {
        var albumId = AlbumId.From("artist-201", "album-401");
        var environment = GetAlbumSociableTestEnvironment.ForNoDataAvailable(albumId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Album_Id_Is_Read()
    {
        var albumId = AlbumId.From("artist-203", "album-403");
        var environment = GetAlbumSociableTestEnvironment.ForNoDataAvailable(albumId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedAlbumIds.Single().Should().Be(albumId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var albumId = AlbumId.From("artist-201", "album-401");
        var environment = GetAlbumSociableTestEnvironment.ForNoDataAvailable(albumId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }
}
