using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.OdesliStreamingReferencesSource;

internal sealed class OdesliStreamingReferencesSourceTestEnvironment : IDisposable
{
    private readonly Action<MusicSearchCriteria, IReadOnlyList<ExternalReference>> seed;
    private readonly IDisposable? cleanup;

    private OdesliStreamingReferencesSourceTestEnvironment(
        IGetMusicTrackReference source,
        Action<MusicSearchCriteria, IReadOnlyList<ExternalReference>> seed,
        IDisposable? cleanup = null)
    {
        Source = source;
        this.seed = seed;
        this.cleanup = cleanup;
    }

    public IGetMusicTrackReference Source { get; }

    public static OdesliStreamingReferencesSourceTestEnvironment Create(OdesliStreamingReferencesSourceMode mode) =>
        mode switch
        {
            OdesliStreamingReferencesSourceMode.InProcessFake => CreateFake(),
            OdesliStreamingReferencesSourceMode.HttpAdapter => CreateHttpAdapter(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(MusicSearchCriteria lookupKey, params ExternalReference[] references) => seed(lookupKey, references);

    private static OdesliStreamingReferencesSourceTestEnvironment CreateFake()
    {
        var fake = new FakeGetMusicTrackReference();
        return new OdesliStreamingReferencesSourceTestEnvironment(
            fake,
            (lookupKey, references) => fake.Seed(lookupKey, references.ToArray()));
    }

    private static OdesliStreamingReferencesSourceTestEnvironment CreateHttpAdapter()
    {
        var server = new WireMockMusicProvidersServer();

        var options = Options.Create(new OdesliOptions
        {
            BaseUrl = server.BaseUrl,
            UserCountry = "US"
        });
        var client = new HttpClient();
        OdesliStreamingReferences.ConfigureHttpClient(client, options.Value);

        return new OdesliStreamingReferencesSourceTestEnvironment(
            new OdesliStreamingReferences(client, options),
            (searchTerm, references) => server.SeedOdesli(searchTerm, references, options.Value.UserCountry),
            server);
    }

    public void Dispose()
    {
        cleanup?.Dispose();
    }
}
