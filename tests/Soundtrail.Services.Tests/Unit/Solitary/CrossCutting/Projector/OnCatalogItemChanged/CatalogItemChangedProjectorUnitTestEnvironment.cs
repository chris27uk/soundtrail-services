using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Unit.Solitary.CrossCutting.Projector.OnCatalogItemChanged;

internal sealed class CatalogItemChangedProjectorUnitTestEnvironment
{
    private CatalogItemChangedProjectorUnitTestEnvironment(
        StorePlaylistTracksReadModelPortFake storePlaylistTracksReadModelPort)
    {
        StorePlaylistTracksReadModelPort = storePlaylistTracksReadModelPort;
    }

    public StorePlaylistTracksReadModelPortFake StorePlaylistTracksReadModelPort { get; }

    public static CatalogItemChangedProjectorUnitTestEnvironment Create() =>
        new(StorePlaylistTracksReadModelPortFake.ForRepairTracking());

    public CatalogTrackChangedProjectorHandler CreateCatalogTrackChangedSubject() =>
        new(StorePlaylistTracksReadModelPort);
}
