using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

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

        return EnsureEntityJsonlAsync(ArtistEntity, dumpVersion, allowHttpDownload: true, cancellationToken);
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

        return EnsureEntityJsonlAsync(ReleaseGroupEntity, dumpVersion, allowHttpDownload: true, cancellationToken);
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

        return EnsureEntityJsonlAsync(ReleaseEntity, dumpVersion, allowHttpDownload: true, cancellationToken);
    }

    public async Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value.TracksLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return RequireExistingPath(configured, TrackEntity);
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "track.jsonl");
            if (File.Exists(sibling))
            {
                return RequireExistingPath(sibling, TrackEntity);
            }
        }

        // Prebuilt denormalized track archive/fixture (Soundtrail-specific; never HTTP-download "track").
        if (TryGetArchiveExtractedPath(TrackEntity, dumpVersion, out var trackExtractedPath) &&
            File.Exists(trackExtractedPath))
        {
            return trackExtractedPath;
        }

        var trackArchivePath = TryGetArchivePath(TrackEntity, dumpVersion);
        if (trackArchivePath is not null && File.Exists(trackArchivePath))
        {
            return await EnsureEntityJsonlAsync(
                TrackEntity,
                dumpVersion,
                allowHttpDownload: false,
                cancellationToken);
        }

        // Materialize denormalized track JSONL from the official release graph.
        var releasesPath = await EnsureReleasesJsonlAsync(jobId, dumpVersion, cancellationToken);
        var outputPath = RequireArchiveExtractedPath(TrackEntity, dumpVersion);
        await MusicBrainzReleaseGraphTrackJoiner.WriteJoinedTracksAsync(
            releasesPath,
            outputPath,
            cancellationToken);
        return outputPath;
    }

    private async Task<string> EnsureEntityJsonlAsync(
        string entityName,
        string dumpVersion,
        bool allowHttpDownload,
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
            if (!allowHttpDownload || !IsHttpSource())
            {
                throw new FileNotFoundException(
                    $"MusicBrainz {entityName} archive was not found at '{archivePath}'.",
                    archivePath);
            }

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

    private bool IsHttpSource() =>
        string.Equals(options.Value.Source, "http", StringComparison.OrdinalIgnoreCase);

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
                $"MusicBrainzDump path for {label} must be set when Source=local.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"MusicBrainz {label} JSONL fixture was not found at '{fullPath}'.",
                fullPath);
        }

        return fullPath;
    }
}

public sealed class LocalMusicBrainzDumpShardStore(IOptions<MusicBrainzDumpOptions> options)
    : IMusicBrainzDumpShardStore
{
    public async Task WriteShardAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken = default)
    {
        var path = ShardPath(jobId, phase, shardId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllLinesAsync(path, lines, cancellationToken);
    }

    public async IAsyncEnumerable<string> ReadShardLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long skipLines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = ShardPath(jobId, phase, shardId);
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

    private string ShardPath(
        MusicBrainzDumpImportJobId jobId,
        MusicBrainzDumpImportPhase phase,
        int shardId)
    {
        var root = string.IsNullOrWhiteSpace(options.Value.ShardDirectory)
            ? Path.Combine(Path.GetTempPath(), "soundtrail-mb-shards")
            : options.Value.ShardDirectory!;

        var safeJob = jobId.Value.Replace(':', '_');
        return Path.Combine(root, safeJob, phase.ToString(), $"{shardId}.jsonl");
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
