using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;

public sealed class HttpMusicBrainzDumpSnapshotCatalog(
    HttpClient httpClient,
    IOptions<MusicBrainzDumpOptions> options) : IMusicBrainzDumpSnapshotCatalog
{
    private static readonly string[] RequiredEntities = ["artist", "release-group", "release"];

    private static readonly Regex HrefDirectory = new(
        """href=["'](?<name>[^"'/]+)/["']""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<MusicBrainzDumpSnapshotId> GetLatestSnapshotIdAsync(
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();
        var latestUrl = $"{baseUrl}/LATEST";
        using var latestResponse = await httpClient.GetAsync(latestUrl, cancellationToken);
        if (latestResponse.IsSuccessStatusCode)
        {
            var body = (await latestResponse.Content.ReadAsStringAsync(cancellationToken)).Trim();
            if (MusicBrainzDumpSnapshotId.TryParse(body, out var fromLatest))
            {
                return fromLatest;
            }
        }

        using var indexResponse = await httpClient.GetAsync(baseUrl + "/", cancellationToken);
        indexResponse.EnsureSuccessStatusCode();
        var html = await indexResponse.Content.ReadAsStringAsync(cancellationToken);
        var candidates = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Match match in HrefDirectory.Matches(html))
        {
            var name = match.Groups["name"].Value;
            if (MusicBrainzDumpSnapshotId.TryParse(name, out var id))
            {
                candidates.Add(id.Value);
            }
        }

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException(
                $"No MusicBrainz dump snapshot directories were found at '{baseUrl}'.");
        }

        return MusicBrainzDumpSnapshotId.Parse(candidates.Max!);
    }

    public async Task<bool> SnapshotExistsAsync(
        MusicBrainzDumpSnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = RequireBaseUrl();
        foreach (var entity in RequiredEntities)
        {
            var url = $"{baseUrl}/{snapshotId.Value}/{entity}.tar.xz";
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                using var getResponse = await httpClient.SendAsync(
                    getRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (!getResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
        }

        return true;
    }

    private string RequireBaseUrl()
    {
        var baseUrl = options.Value.BaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("MusicBrainzDump:BaseUrl must be set.");
        }

        return baseUrl;
    }
}
