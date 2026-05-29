using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Integration.Features.CatalogLookup.Contracts
{
    internal sealed class FakeCatalogLookupPort : ICatalogLookupPort, ISeedCatalogLookup
    {
        private readonly List<Track> tracks = [];

        public Task<Track?> LookupAsync(CatalogLookupRequest request, CancellationToken cancellationToken)
        {
            var track = this.tracks.SingleOrDefault(candidate => Matches(request, candidate));
            return Task.FromResult(track);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public void Seed(params Track[] tracks)
        {
            this.tracks.Clear();
            this.tracks.AddRange(tracks);
        }

        private static bool Matches(CatalogLookupRequest request, Track track) =>
            request switch
            {
                { Isrc: not null } => track.Isrc?.Value == request.Isrc,
                { AppleId: not null } => track.AppleId?.Value == request.AppleId,
                { Mbid: not null } => track.Mbid?.Value == request.Mbid,
                { SpotifyId: not null } => track.SpotifyId?.Value == request.SpotifyId,
                _ => false
            };
    }
}
