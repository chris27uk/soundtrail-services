using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;

public sealed class OdesliStreamingLocationPort(
    HttpClient httpClient,
    IOptions<OdesliOptions> options) : IReadStreamingLocationByProviderPort
{
    public const string HttpClientName = "OdesliStreamingLocation";

    private readonly OdesliOptions options = options.Value;

    public Task<Uri?> ReadByIsrcAsync(
        string isrc,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["platform"] = "isrc",
            ["type"] = "song",
            ["id"] = isrc,
            ["songIfSingle"] = "true",
            ["userCountry"] = options.UserCountry
        };

        return ReadAsync(query, provider, cancellationToken);
    }

    public Task<Uri?> ReadByTrackMetadataAsync(
        string artistName,
        string trackTitle,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["artistName"] = artistName,
            ["songName"] = trackTitle,
            ["songIfSingle"] = "true",
            ["userCountry"] = options.UserCountry
        };

        return ReadAsync(query, provider, cancellationToken);
    }

    private async Task<Uri?> ReadAsync(
        IReadOnlyDictionary<string, string?> query,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        var path = QueryHelpers.AddQueryString("/v1-user/links", query!);
        using var response = await httpClient.GetAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OdesliLookupResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Odesli response body is required.");

        if (payload.LinksByPlatform is null)
        {
            throw new InvalidOperationException("Odesli response must include linksByPlatform.");
        }

        if (!payload.LinksByPlatform.TryGetValue(provider.StableValue, out var link) || string.IsNullOrWhiteSpace(link?.Url))
        {
            return null;
        }

        if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Odesli provider link must be an absolute URL.");
        }

        return uri;
    }

    private sealed class OdesliLookupResponse
    {
        public Dictionary<string, OdesliProviderLink>? LinksByPlatform { get; init; }
    }

    private sealed class OdesliProviderLink
    {
        public string? Url { get; init; }
    }
}
