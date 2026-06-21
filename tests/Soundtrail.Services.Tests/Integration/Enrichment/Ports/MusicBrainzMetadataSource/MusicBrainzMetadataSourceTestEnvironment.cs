using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

internal sealed class MusicBrainzMetadataSourceTestEnvironment : IDisposable
{
    private readonly Action<MusicSearchTerm, SongMetadata> seed;
    private readonly Action<MusicSearchTerm>? seedAmbiguous;
    private readonly Action<MusicSearchTerm, SongMetadata>? seedPreferredMatch;
    private readonly IDisposable? cleanup;

    private MusicBrainzMetadataSourceTestEnvironment(
        IGetCanonicalMusicMetadata source,
        Action<MusicSearchTerm, SongMetadata> seed,
        Action<MusicSearchTerm>? seedAmbiguous,
        Action<MusicSearchTerm, SongMetadata>? seedPreferredMatch,
        IDisposable? cleanup = null)
    {
        Source = source;
        this.seed = seed;
        this.seedAmbiguous = seedAmbiguous;
        this.seedPreferredMatch = seedPreferredMatch;
        this.cleanup = cleanup;
    }

    public IGetCanonicalMusicMetadata Source { get; }

    public static MusicBrainzMetadataSourceTestEnvironment Create(MusicBrainzMetadataSourceMode mode) =>
        mode switch
        {
            MusicBrainzMetadataSourceMode.InProcessFake => CreateFake(),
            MusicBrainzMetadataSourceMode.HttpAdapter => CreateHttpAdapter(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(MusicSearchTerm searchTerm, SongMetadata metadata) => seed(searchTerm, metadata);

    public void SeedAmbiguous(MusicSearchTerm searchTerm) => seedAmbiguous?.Invoke(searchTerm);

    public void SeedPreferredMatch(MusicSearchTerm searchTerm, SongMetadata metadata) => seedPreferredMatch?.Invoke(searchTerm, metadata);

    private static MusicBrainzMetadataSourceTestEnvironment CreateFake()
    {
        var fake = new FakeGetCanonicalMusicMetadata();
        return new MusicBrainzMetadataSourceTestEnvironment(
            fake,
            (searchTerm, metadata) =>
            {
                searchTerm.Match(
                    (track, artist, album) => fake.SeedNames(track, artist, album, metadata),
                    isrc => fake.SeedIsrc(isrc, metadata));
            },
            seedAmbiguous: _ => { },
            seedPreferredMatch: (searchTerm, metadata) =>
            {
                searchTerm.Match(
                    (track, artist, album) => fake.SeedNames(track, artist, album, metadata),
                    isrc => fake.SeedIsrc(isrc, metadata));
            });
    }

    private static MusicBrainzMetadataSourceTestEnvironment CreateHttpAdapter()
    {
        var server = new WireMockMusicProvidersServer();
        var options = Options.Create(new MusicBrainzOptions { BaseUrl = server.BaseUrl, UserAgent = "Soundtrail.Tests/1.0" });
        var client = new HttpClient();
        MusicBrainzGetCanonicalMusicMetadata.ConfigureHttpClient(client, options.Value);

        return new MusicBrainzMetadataSourceTestEnvironment(
            new MusicBrainzGetCanonicalMusicMetadata(client),
            server.SeedMusicBrainz,
            seedAmbiguous: searchTerm =>
            {
                searchTerm.Match(
                    (track, artist, album) =>
                    {
                        server.SeedMusicBrainzSearchResponse(
                            ("mbid-a", track, 123000, "100", Array.Empty<string>(), artist, "mb-artist-a", album, "mb-release-a", "2004-06-07"),
                            ("mbid-b", track, 123000, "99", Array.Empty<string>(), artist, "mb-artist-b", album, "mb-release-b", "2004-06-08"));
                        return 0;
                    },
                    isrc =>
                    {
                        server.SeedMusicBrainzIsrcResponse(
                            isrc,
                            ("mbid-a", "Song A", 123000, new[] { isrc }, "Artist A", "mb-artist-a", "Album A", "mb-release-a", "2004-06-07"),
                            ("mbid-b", "Song A", 123000, new[] { isrc }, "Artist A", "mb-artist-b", "Album B", "mb-release-b", "2004-06-08"));
                        return 0;
                    });
            },
            seedPreferredMatch: (searchTerm, metadata) =>
            {
                searchTerm.Match(
                    (track, artist, album) =>
                    {
                        server.SeedMusicBrainzSearchResponse(
                            ("mbid-other", track, metadata.DurationMs, "100", Array.Empty<string>(), artist, "mb-artist-other", "Different Album", "mb-release-other", "2005-01-01"),
                            (metadata.Mbid, metadata.Title, metadata.DurationMs, "95", Array.Empty<string>(), metadata.Artist, metadata.SourceArtistId, album, metadata.SourceAlbumId, "2004-06-07"));
                        return 0;
                    },
                    isrc =>
                    {
                        server.SeedMusicBrainz(MusicSearchTerm.ByIsrc(isrc), metadata);
                        return 0;
                    });
            },
            server);
    }

    public void Dispose()
    {
        cleanup?.Dispose();
    }
}
