using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicBrainzMetadataSource;

internal sealed class MusicBrainzMetadataSourceTestEnvironment : IDisposable
{
    private readonly Action<MusicSearchTerm, SongMetadata> seed;
    private readonly IDisposable? cleanup;

    private MusicBrainzMetadataSourceTestEnvironment(
        IGetCanonicalMusicMetadata source,
        Action<MusicSearchTerm, SongMetadata> seed,
        IDisposable? cleanup = null)
    {
        Source = source;
        this.seed = seed;
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
        var server = new WireMockMusicProvidersServer();
        var options = Options.Create(new MusicBrainzOptions { BaseUrl = server.BaseUrl, UserAgent = "Soundtrail.Tests/1.0" });
        var client = new HttpClient();
        MusicBrainzGetCanonicalMusicMetadata.ConfigureHttpClient(client, options.Value);

        return new MusicBrainzMetadataSourceTestEnvironment(
            new MusicBrainzGetCanonicalMusicMetadata(client),
            server.SeedMusicBrainz,
            server);
    }

    public void Dispose()
    {
        cleanup?.Dispose();
    }
}
