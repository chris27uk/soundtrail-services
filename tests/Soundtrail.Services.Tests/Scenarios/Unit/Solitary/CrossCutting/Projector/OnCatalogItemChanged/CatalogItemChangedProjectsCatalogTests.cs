namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Projector.OnCatalogItemChanged;

public sealed class CatalogItemChangedProjectsCatalogTests
{
    [Fact]
    public async Task Given_A_Catalog_Track_Change_When_Projecting_Then_Referencing_Playlists_Are_Repaired()
    {
        var environment = CatalogItemChangedProjectorUnitTestEnvironment.Create();
        var subject = environment.CreateCatalogTrackChangedSubject();
        var trackId = TestTrackIds.Create("projected-playlist-track");

        await subject.Handle(trackId);

        environment.StorePlaylistTracksReadModelPort.RepairedTrackId.Should().Be(trackId);
    }
}
