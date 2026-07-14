namespace Soundtrail.Domain.Catalog.Playlists
{
    public record PlaylistId
    {
        private PlaylistId(string value) => this.Value = value;

        public string Value { get; }

        public static PlaylistId FromPlaylistName(string playlistName) => new(MusicIdentityText.NormalizeCompact(playlistName));
    }
}
