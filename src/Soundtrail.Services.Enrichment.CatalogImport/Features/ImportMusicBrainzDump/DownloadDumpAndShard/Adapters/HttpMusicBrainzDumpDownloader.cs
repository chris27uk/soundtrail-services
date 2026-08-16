using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class HttpMusicBrainzDumpDownloader(HttpClient httpClient) : Ports.IMusicBrainzDumpDownloader
{
    private const int BufferSize = 81_920;

    public async Task DownloadAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (File.Exists(destinationPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);

        var tempPath = destinationPath + ".partial";
        var metaPath = destinationPath + ".partial.meta";
        var existingLength = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;
        var validators = existingLength > 0 ? ReadValidators(metaPath) : default;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (existingLength > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existingLength, null);
            if (!string.IsNullOrWhiteSpace(validators.ETag) &&
                EntityTagHeaderValue.TryParse(validators.ETag, out var etag))
            {
                request.Headers.IfRange = new RangeConditionHeaderValue(etag);
            }
            else if (validators.LastModified is { } lastModified)
            {
                request.Headers.IfRange = new RangeConditionHeaderValue(lastModified);
            }
        }

        using var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && existingLength > 0)
        {
            // Already have the full object (or server cannot satisfy the range). Prefer completing when sizes match.
            var totalLength = TryGetTotalLength(response);
            if (totalLength is null || totalLength == existingLength)
            {
                File.Move(tempPath, destinationPath, overwrite: true);
                DeleteIfExists(metaPath);
                return;
            }

            response.EnsureSuccessStatusCode();
        }

        if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.PartialContent))
        {
            response.EnsureSuccessStatusCode();
        }

        var append = response.StatusCode == HttpStatusCode.PartialContent && existingLength > 0;
        if (!append && File.Exists(tempPath))
        {
            File.Delete(tempPath);
            existingLength = 0;
        }

        WriteValidators(metaPath, response);

        await using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(
                   tempPath,
                   append ? FileMode.Append : FileMode.Create,
                   FileAccess.Write,
                   FileShare.None,
                   BufferSize,
                   FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }

        File.Move(tempPath, destinationPath, overwrite: true);
        DeleteIfExists(metaPath);
    }

    private static long? TryGetTotalLength(HttpResponseMessage response)
    {
        var contentRange = response.Content.Headers.ContentRange;
        if (contentRange?.Length is { } length)
        {
            return length;
        }

        return response.Content.Headers.ContentLength;
    }

    private static (string? ETag, DateTimeOffset? LastModified) ReadValidators(string metaPath)
    {
        if (!File.Exists(metaPath))
        {
            return default;
        }

        string? etag = null;
        DateTimeOffset? lastModified = null;
        foreach (var line in File.ReadLines(metaPath))
        {
            if (line.StartsWith("etag=", StringComparison.OrdinalIgnoreCase))
            {
                etag = line["etag=".Length..].Trim();
            }
            else if (line.StartsWith("last-modified=", StringComparison.OrdinalIgnoreCase) &&
                     DateTimeOffset.TryParse(
                         line["last-modified=".Length..].Trim(),
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.RoundtripKind,
                         out var parsed))
            {
                lastModified = parsed;
            }
        }

        return (etag, lastModified);
    }

    private static void WriteValidators(string metaPath, HttpResponseMessage response)
    {
        var etag = response.Headers.ETag?.ToString();
        var lastModified = response.Content.Headers.LastModified;
        if (string.IsNullOrWhiteSpace(etag) && lastModified is null)
        {
            DeleteIfExists(metaPath);
            return;
        }

        using var writer = new StreamWriter(metaPath, append: false);
        if (!string.IsNullOrWhiteSpace(etag))
        {
            writer.WriteLine($"etag={etag}");
        }

        if (lastModified is { } value)
        {
            writer.WriteLine($"last-modified={value.ToString("O", CultureInfo.InvariantCulture)}");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
