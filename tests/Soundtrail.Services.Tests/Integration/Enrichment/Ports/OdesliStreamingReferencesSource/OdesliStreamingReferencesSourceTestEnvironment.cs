using Microsoft.Extensions.Options;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Providers.Odesli;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using OdesliAdapter = Soundtrail.Services.Enrichment.Worker.Infrastructure.Providers.Odesli.OdesliStreamingReferencesSource;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.OdesliStreamingReferencesSource;

internal sealed class OdesliStreamingReferencesSourceTestEnvironment
{
    private readonly Action<PlaybackReferenceLookupKey, IReadOnlyList<ExternalReference>> seed;

    private OdesliStreamingReferencesSourceTestEnvironment(
        IPlaybackReferenceSource source,
        Action<PlaybackReferenceLookupKey, IReadOnlyList<ExternalReference>> seed)
    {
        Source = source;
        this.seed = seed;
    }

    public IPlaybackReferenceSource Source { get; }

    public static OdesliStreamingReferencesSourceTestEnvironment Create(OdesliStreamingReferencesSourceMode mode) =>
        mode switch
        {
            OdesliStreamingReferencesSourceMode.InProcessFake => CreateFake(),
            OdesliStreamingReferencesSourceMode.HttpAdapter => CreateHttpAdapter(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

    public void Seed(PlaybackReferenceLookupKey lookupKey, params ExternalReference[] references) => seed(lookupKey, references);

    private static OdesliStreamingReferencesSourceTestEnvironment CreateFake()
    {
        var fake = new FakePlaybackReferenceSource();
        return new OdesliStreamingReferencesSourceTestEnvironment(
            fake,
            (lookupKey, references) => fake.Seed(lookupKey, references.ToArray()));
    }

    private static OdesliStreamingReferencesSourceTestEnvironment CreateHttpAdapter()
    {
        var responses = new Dictionary<string, IReadOnlyList<ExternalReference>>(StringComparer.OrdinalIgnoreCase);
        var handler = new StubHttpMessageHandler(request =>
        {
            var references = responses[request.RequestUri!.PathAndQuery];
            var youtube = references.SingleOrDefault(x => x.Provider == Soundtrail.Contracts.Common.ProviderName.YoutubeMusic);
            var spotify = references.SingleOrDefault(x => x.Provider == Soundtrail.Contracts.Common.ProviderName.Spotify);
            var apple = references.SingleOrDefault(x => x.Provider == Soundtrail.Contracts.Common.ProviderName.AppleMusic);
            var platforms = new List<string>();

            if (youtube is not null)
            {
                platforms.Add($"\"youtubeMusic\":{{\"url\":\"{youtube.Url}\"}}");
            }

            if (spotify is not null)
            {
                platforms.Add($"\"spotify\":{{\"url\":\"{spotify.Url}\"}}");
            }

            if (apple is not null)
            {
                platforms.Add($"\"appleMusic\":{{\"url\":\"{apple.Url}\"}}");
            }

            return StubHttpMessageHandler.Json(
                $"{{\"linksByPlatform\":{{{string.Join(",", platforms)}}}}}");
        });

        var options = Options.Create(new OdesliOptions
        {
            BaseUrl = "https://song.link.test",
            UserCountry = "US"
        });
        var client = new HttpClient(handler);
        OdesliAdapter.ConfigureHttpClient(client, options.Value);

        return new OdesliStreamingReferencesSourceTestEnvironment(
            new OdesliAdapter(client, options),
            (lookupKey, references) =>
            {
                var key = lookupKey.Mode switch
                {
                    PlaybackReferenceLookupMode.Isrc => $"/v1-user/links?id={Uri.EscapeDataString(lookupKey.Isrc ?? string.Empty)}&platform=isrc&userCountry=US",
                    PlaybackReferenceLookupMode.ByTrackNameAndArtist => $"/v1-user/links?title={Uri.EscapeDataString(lookupKey.Title ?? string.Empty)}&artist={Uri.EscapeDataString(lookupKey.Artist ?? string.Empty)}&userCountry=US",
                    _ => throw new ArgumentOutOfRangeException(nameof(lookupKey.Mode), lookupKey.Mode, null)
                };
                responses[key] = references;
            });
    }
}
