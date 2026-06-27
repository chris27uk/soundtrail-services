using Microsoft.Extensions.Options;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

internal sealed class MusicBrainzMetadataSourceTestEnvironment : IDisposable
{
    private readonly Action<MusicSearchCriteria, SongMetadata> seed;
    private readonly Action<MusicSearchCriteria>? seedAmbiguous;
    private readonly Action<MusicSearchCriteria, SongMetadata>? seedPreferredMatch;
    private readonly IDisposable? cleanup;

    private MusicBrainzMetadataSourceTestEnvironment(
        IGetTrackMetadata source,
        Action<MusicSearchCriteria, SongMetadata> seed,
        Action<MusicSearchCriteria>? seedAmbiguous,
        Action<MusicSearchCriteria, SongMetadata>? seedPreferredMatch,
        IDisposable? cleanup = null)
    {
        Source = source;
        this.seed = seed;
        this.seedAmbiguous = seedAmbiguous;
        this.seedPreferredMatch = seedPreferredMatch;
        this.cleanup = cleanup;
    }

    public IGetTrackMetadata Source { get; }

    public static MusicBrainzMetadataSourceTestEnvironment Create(MusicBrainzMetadataSourceMode mode) =>
        mode switch
        {
            MusicBrainzMetadataSourceMode.InProcessFake => CreateFake(),
            MusicBrainzMetadataSourceMode.HttpAdapter => CreateHttpAdapter(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(MusicSearchCriteria searchCriteria, SongMetadata metadata) => seed(searchCriteria, metadata);

    public void SeedAmbiguous(MusicSearchCriteria searchCriteria) => seedAmbiguous?.Invoke(searchCriteria);

    public void SeedPreferredMatch(MusicSearchCriteria searchCriteria, SongMetadata metadata) => seedPreferredMatch?.Invoke(searchCriteria, metadata);

    private static MusicBrainzMetadataSourceTestEnvironment CreateFake()
    {
        var fake = new FakeGetMusicMetadata();
        return new MusicBrainzMetadataSourceTestEnvironment(
            fake,
            (searchTerm, metadata) =>
            {
                searchTerm.Match(
                    (track, artist, album) => fake.SeedNames(track, artist, album, metadata),
                    isrc => fake.SeedIsrc(isrc, metadata));
            },
            seedAmbiguous: searchTerm =>
            {
                searchTerm.Match(
                    (track, artist, album) =>
                    {
                        fake.SeedAmbiguousNames(track, artist, album);
                        return 0;
                    },
                    isrc =>
                    {
                        fake.SeedAmbiguousIsrc(isrc);
                        return 0;
                    });
            },
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
        MusicBrainzGetTrackMetadata.ConfigureHttpClient(client, options.Value);

        return new MusicBrainzMetadataSourceTestEnvironment(
            new MusicBrainzGetTrackMetadata(client),
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
                        server.SeedMusicBrainz(MusicSearchCriteria.ByIsrc(isrc), metadata);
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
