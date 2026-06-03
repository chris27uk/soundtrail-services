using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Api.Integration.Infrastructure
{
    public sealed class ApiFakeCatalogLookupPort : ICatalogLookupPort
    {
        private readonly List<Track> tracks = [];

        public bool Ready { get; set; } = true;

        public Task<Track?> LookupAsync(CatalogLookupRequest request, CancellationToken cancellationToken)
        {
            var track = this.tracks.SingleOrDefault(candidate => Matches(request, candidate));
            return Task.FromResult(track);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);

        public void Seed(params Track[] tracks)
        {
            this.tracks.Clear();
            this.tracks.AddRange(tracks);
        }

        public void Clear() => this.tracks.Clear();

        private static bool Matches(CatalogLookupRequest request, Track track) =>
            MatchesIdentifier(request, track) &&
            MatchesDuration(request, track);

        private static bool MatchesIdentifier(CatalogLookupRequest request, Track track) =>
            request switch
            {
                { Isrc: not null } => track.Isrc?.Value == request.Isrc,
                { AppleId: not null } => track.AppleId?.Value == request.AppleId,
                { Mbid: not null } => track.Mbid?.Value == request.Mbid,
                { SpotifyId: not null } => track.SpotifyId?.Value == request.SpotifyId,
                _ => false
            };

        private static bool MatchesDuration(CatalogLookupRequest request, Track track) =>
            request.DurationMs is null ||
            track.Duration?.Value == request.DurationMs.Value.Value;
    }
}
