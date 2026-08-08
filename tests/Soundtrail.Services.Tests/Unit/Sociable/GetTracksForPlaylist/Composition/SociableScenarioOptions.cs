using Soundtrail.Domain.Catalog.Playlists;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition
{
    internal sealed class SociableScenarioOptions(DateTimeOffset utcNow)
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public PlaylistId PlaylistId { get; } = PlaylistId.FromPlaylistName("world_top_100");
    }
}
