using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class LocalMusicBrainzDumpArchiveStore(
    IOptions<MusicBrainzDumpOptions> options,
    IMusicBrainzDumpDownloader downloader,
    IMusicBrainzDumpTarXzExtractor extractor) : IMusicBrainzDumpArchiveStore
{
    public const string ArtistEntity = "artist";
    public const string ReleaseGroupEntity = "release-group";
    public const string ReleaseEntity = "release";
    public const string TrackEntity = "track";

    public Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        var configured = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, ArtistEntity));
        }

        return EnsureOfficialEntityJsonlAsync(ArtistEntity, dumpVersion, cancellationToken);
    }

    public Task<string> EnsureReleaseGroupsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;

        var configured = options.Value.ReleaseGroupsLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, ReleaseGroupEntity));
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "release-group.jsonl");
            if (File.Exists(sibling))
            {
                return Task.FromResult(RequireExistingPath(sibling, ReleaseGroupEntity));
            }
        }

        return EnsureOfficialEntityJsonlAsync(ReleaseGroupEntity, dumpVersion, cancellationToken);
    }

    public Task<string> EnsureReleasesJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;

        var configured = options.Value.ReleasesLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, ReleaseEntity));
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "release.jsonl");
            if (File.Exists(sibling))
            {
                return Task.FromResult(RequireExistingPath(sibling, ReleaseEntity));
            }
        }

        return EnsureOfficialEntityJsonlAsync(ReleaseEntity, dumpVersion, cancellationToken);
    }

    public async Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        if (TryGetCachedTrackJsonlPath(dumpVersion, out var cachedPath))
        {
            return cachedPath;
        }

        var outputPath = RequireArchiveExtractedPath(TrackEntity, dumpVersion);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        await using (var output = new StreamWriter(outputPath))
        {
            await foreach (var line in ReadTracksFromReleaseGraphAsync(jobId, dumpVersion, cancellationToken))
            {
                await output.WriteLineAsync(line);
            }
        }

        return outputPath;
    }

    public async IAsyncEnumerable<string> ReadDenormalizedTrackLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (TryGetCachedTrackJsonlPath(dumpVersion, out var cachedPath))
        {
            await foreach (var line in File.ReadLinesAsync(cachedPath, cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        var trackArchivePath = TryGetArchivePath(TrackEntity, dumpVersion);
        if (trackArchivePath is not null && File.Exists(trackArchivePath))
        {
            await foreach (var line in extractor.ReadJsonlLinesAsync(
                               trackArchivePath,
                               TrackEntity,
                               cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        await foreach (var line in ReadTracksFromReleaseGraphAsync(jobId, dumpVersion, cancellationToken))
        {
            yield return line;
        }
    }

    private bool TryGetCachedTrackJsonlPath(string dumpVersion, out string path)
    {
        var configured = options.Value.TracksLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            path = RequireExistingPath(configured, TrackEntity);
            return true;
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "track.jsonl");
            if (File.Exists(sibling))
            {
                path = RequireExistingPath(sibling, TrackEntity);
                return true;
            }
        }

        if (TryGetArchiveExtractedPath(TrackEntity, dumpVersion, out var trackExtractedPath) &&
            File.Exists(trackExtractedPath))
        {
            path = trackExtractedPath;
            return true;
        }

        var trackArchivePath = TryGetArchivePath(TrackEntity, dumpVersion);
        if (trackArchivePath is not null && File.Exists(trackArchivePath))
        {
            path = RequireArchiveExtractedPath(TrackEntity, dumpVersion);
            extractor.EnsureExtracted(trackArchivePath, TrackEntity, path);
            return true;
        }

        path = string.Empty;
        return false;
    }

    private async IAsyncEnumerable<string> ReadTracksFromReleaseGraphAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _ = jobId;

        var configured = options.Value.ReleasesLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                               File.ReadLinesAsync(RequireExistingPath(configured, ReleaseEntity), cancellationToken),
                               cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "release.jsonl");
            if (File.Exists(sibling))
            {
                await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                                   File.ReadLinesAsync(sibling, cancellationToken),
                                   cancellationToken))
                {
                    yield return line;
                }

                yield break;
            }
        }

        if (TryGetArchiveExtractedPath(ReleaseEntity, dumpVersion, out var extractedRelease) &&
            File.Exists(extractedRelease))
        {
            await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                               File.ReadLinesAsync(extractedRelease, cancellationToken),
                               cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        var archivePath = await EnsureOfficialArchiveOnDiskAsync(ReleaseEntity, dumpVersion, cancellationToken);
        await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                           extractor.ReadJsonlLinesAsync(archivePath, ReleaseEntity, cancellationToken),
                           cancellationToken))
        {
            yield return line;
        }
    }

    private async Task<string> EnsureOfficialArchiveOnDiskAsync(
        string entityName,
        string dumpVersion,
        CancellationToken cancellationToken)
    {
        var archivePath = RequireArchivePath(entityName, dumpVersion);
        if (!File.Exists(archivePath))
        {
            var url = BuildDownloadUrl(dumpVersion, entityName);
            await downloader.DownloadAsync(url, archivePath, cancellationToken);
        }

        return archivePath;
    }

    private async Task<string> EnsureOfficialEntityJsonlAsync(
        string entityName,
        string dumpVersion,
        CancellationToken cancellationToken)
    {
        var extractedPath = RequireArchiveExtractedPath(entityName, dumpVersion);
        if (File.Exists(extractedPath))
        {
            return extractedPath;
        }

        var archivePath = RequireArchivePath(entityName, dumpVersion);
        if (!File.Exists(archivePath))
        {
            var url = BuildDownloadUrl(dumpVersion, entityName);
            await downloader.DownloadAsync(url, archivePath, cancellationToken);
        }

        extractor.EnsureExtracted(archivePath, entityName, extractedPath);
        return extractedPath;
    }

    private bool TryGetArchiveExtractedPath(string entityName, string dumpVersion, out string extractedPath)
    {
        extractedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(options.Value.ArchiveDirectory) ||
            string.IsNullOrWhiteSpace(dumpVersion))
        {
            return false;
        }

        extractedPath = Path.Combine(
            Path.GetFullPath(options.Value.ArchiveDirectory),
            dumpVersion.Trim(),
            "extracted",
            $"{entityName}.jsonl");
        return true;
    }

    private string? TryGetArchivePath(string entityName, string dumpVersion)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ArchiveDirectory) ||
            string.IsNullOrWhiteSpace(dumpVersion))
        {
            return null;
        }

        return Path.Combine(
            Path.GetFullPath(options.Value.ArchiveDirectory),
            dumpVersion.Trim(),
            $"{entityName}.tar.xz");
    }

    private string RequireArchiveExtractedPath(string entityName, string dumpVersion)
    {
        if (!TryGetArchiveExtractedPath(entityName, dumpVersion, out var extractedPath))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{entityName}' from archives.");
        }

        return extractedPath;
    }

    private string RequireArchivePath(string entityName, string dumpVersion)
    {
        var archivePath = TryGetArchivePath(entityName, dumpVersion);
        if (archivePath is null)
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{entityName}' from archives.");
        }

        return archivePath;
    }

    private string BuildDownloadUrl(string dumpVersion, string entityName)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.Value.BaseUrl)
            ? "https://data.metabrainz.org/pub/musicbrainz/data/json-dumps"
            : options.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/{dumpVersion.Trim()}/{entityName}.tar.xz";
    }

    private static string RequireExistingPath(string? path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump path for {label} must be set when using an explicit JSONL path.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"MusicBrainz {label} JSONL was not found at '{fullPath}'.",
                fullPath);
        }

        return fullPath;
    }
}

public sealed class LocalMusicBrainzDumpShardStore(IOptions<MusicBrainzDumpOptions> options)
    : IMusicBrainzDumpShardStore
{
    public IMusicBrainzDumpShardWriter OpenWriter(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardCount) =>
        FileMusicBrainzDumpShardWriter.Open(
            jobId,
            phase,
            shardCount,
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory));

    public async IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = FileMusicBrainzDumpShardWriter.ShardFilePath(
            FileMusicBrainzDumpShardWriter.ResolveShardDirectory(options.Value.ShardDirectory),
            jobId,
            phase,
            shardId);
        if (!File.Exists(path))
        {
            yield break;
        }

        long lineNumber = 0;
        await foreach (var line in File.ReadLinesAsync(path, cancellationToken))
        {
            if (lineNumber++ < skipLines)
            {
                continue;
            }

            yield return line;
        }
    }
}

internal static class MusicBrainzArtistJsonLine
{
    public static bool TryReadArtistId(string line, out string artistId)
    {
        artistId = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("id", out var idProperty))
            {
                return false;
            }

            artistId = idProperty.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(artistId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
