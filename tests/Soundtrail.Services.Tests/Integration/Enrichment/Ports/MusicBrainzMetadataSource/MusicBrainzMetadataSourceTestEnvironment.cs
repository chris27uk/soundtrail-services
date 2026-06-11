using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution.Adapters;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

internal sealed class MusicBrainzMetadataSourceTestEnvironment
{
    private readonly Action<MusicSearchTerm, SongMetadata> seed;

    private MusicBrainzMetadataSourceTestEnvironment(
        IGetCanonicalMusicMetadata source,
        Action<MusicSearchTerm, SongMetadata> seed)
    {
        Source = source;
        this.seed = seed;
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
        MusicBrainzGetCanonicalMusicMetadata.ConfigureHttpClient(client, options.Value);

        return new MusicBrainzMetadataSourceTestEnvironment(
            new MusicBrainzGetCanonicalMusicMetadata(client),
            (lookup, metadata) =>
            {
                lookup.Match((track, artist, album) =>
                    {
                        var clauses = new List<string>
                        {
                            $"recording:\"{track}\"",
                            $"artist:\"{artist}\""
                        };

                        if (!string.IsNullOrWhiteSpace(album))
                        {
                            clauses.Add($"release:\"{album}\"");
                        }

                        var query = Uri.EscapeDataString(string.Join(" AND ", clauses));
                        responses[$"/ws/2/recording?fmt=json&limit=5&query={query}&inc=artist-credits+isrcs+releases"] =
                            metadata;
                    },
                    isrc => responses[$"/ws/2/isrc/{Uri.EscapeDataString(isrc)}?fmt=json&inc=artist-credits+isrcs"] = metadata);
            });
    }
}
