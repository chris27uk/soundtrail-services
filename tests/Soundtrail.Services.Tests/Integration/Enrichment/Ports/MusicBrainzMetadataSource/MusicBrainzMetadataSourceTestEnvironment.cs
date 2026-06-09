using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Providers.MusicBrainz;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

internal sealed class MusicBrainzMetadataSourceTestEnvironment
{
    private readonly Action<CanonicalMusicMetadataLookup, SongMetadata> seed;

    private MusicBrainzMetadataSourceTestEnvironment(
        IMusicBrainzMetadataSource source,
        Action<CanonicalMusicMetadataLookup, SongMetadata> seed)
    {
        Source = source;
        this.seed = seed;
    }

    public IMusicBrainzMetadataSource Source { get; }

    public static MusicBrainzMetadataSourceTestEnvironment Create(MusicBrainzMetadataSourceMode mode) =>
        mode switch
        {
            MusicBrainzMetadataSourceMode.InProcessFake => CreateFake(),
            MusicBrainzMetadataSourceMode.HttpAdapter => CreateHttpAdapter(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(CanonicalMusicMetadataLookup lookup, SongMetadata metadata) => seed(lookup, metadata);

    private static MusicBrainzMetadataSourceTestEnvironment CreateFake()
    {
        var fake = new FakeMusicBrainzMetadataSource();
        return new MusicBrainzMetadataSourceTestEnvironment(
            fake,
            (lookup, metadata) =>
            {
                if (lookup is CanonicalMusicMetadataLookup.ByIsrc byIsrc)
                {
                    fake.SeedIsrc(byIsrc.Isrc, metadata);
                }
                else
                {
                    var byTrack = (CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum)lookup;
                    fake.SeedNames(byTrack.TrackName, byTrack.ArtistName, byTrack.AlbumName, metadata);
                }
            });
    }

    private static MusicBrainzMetadataSourceTestEnvironment CreateHttpAdapter()
    {
        var responses = new Dictionary<string, SongMetadata>(StringComparer.OrdinalIgnoreCase);
        var handler = new StubHttpMessageHandler(request =>
        {
            var key = request.RequestUri!.PathAndQuery;
            var metadata = responses[key];

            if (key.Contains("/isrc/", StringComparison.Ordinal))
            {
                return StubHttpMessageHandler.Json($$"""
                {"recordings":[{"id":"{{metadata.Mbid}}","title":"{{metadata.Title}}","length":{{metadata.DurationMs}},"isrcs":["{{metadata.Isrc}}"],"artist-credit":[{"name":"{{metadata.Artist}}"}]}]}
                """);
            }

            return StubHttpMessageHandler.Json($$"""
            {"recordings":[{"id":"{{metadata.Mbid}}","title":"{{metadata.Title}}","length":{{metadata.DurationMs}},"score":"100","isrcs":[],"artist-credit":[{"name":"{{metadata.Artist}}"}]}]}
            """);
        });

        var options = Options.Create(new MusicBrainzOptions { BaseUrl = "https://musicbrainz.test", UserAgent = "Soundtrail.Tests/1.0" });
        var client = new HttpClient(handler);
        MusicBrainzHttpMetadataSource.ConfigureHttpClient(client, options.Value);

        return new MusicBrainzMetadataSourceTestEnvironment(
            new MusicBrainzHttpMetadataSource(client),
            (lookup, metadata) =>
            {
                if (lookup is CanonicalMusicMetadataLookup.ByIsrc byIsrc)
                {
                    responses[$"/ws/2/isrc/{Uri.EscapeDataString(byIsrc.Isrc)}?fmt=json&inc=artist-credits+isrcs"] = metadata;
                }
                else
                {
                    var byTrack = (CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum)lookup;
                    var clauses = new List<string>
                    {
                        $"recording:\"{byTrack.TrackName}\"",
                        $"artist:\"{byTrack.ArtistName}\""
                    };
                    if (!string.IsNullOrWhiteSpace(byTrack.AlbumName))
                    {
                        clauses.Add($"release:\"{byTrack.AlbumName}\"");
                    }

                    var query = Uri.EscapeDataString(string.Join(" AND ", clauses));
                    responses[$"/ws/2/recording?fmt=json&limit=5&query={query}&inc=artist-credits+isrcs+releases"] = metadata;
                }
            });
    }
}
