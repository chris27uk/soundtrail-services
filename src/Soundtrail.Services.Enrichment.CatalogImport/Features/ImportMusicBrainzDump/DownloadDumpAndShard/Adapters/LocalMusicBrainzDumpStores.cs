using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class LocalMusicBrainzDumpArchiveStore(IOptions<MusicBrainzDumpOptions> options)
    : IMusicBrainzDumpArchiveStore
{
    public Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        _ = cancellationToken;
        return Task.FromResult(RequireExistingPath(options.Value.LocalPath, "artists"));
    }

    public Task<string> EnsureReleaseGroupsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        _ = cancellationToken;

        var configured = options.Value.ReleaseGroupsLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, "release-groups"));
        }

        var artistsPath = options.Value.LocalPath;
        if (string.IsNullOrWhiteSpace(artistsPath))
        {
            throw new InvalidOperationException(
                "MusicBrainzDump:LocalPath or ReleaseGroupsLocalPath must be set when Source=local.");
        }

        var sibling = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
            "release-group.jsonl");
        return Task.FromResult(RequireExistingPath(sibling, "release-groups"));
    }

    public Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        _ = dumpVersion;
        _ = cancellationToken;

        var configured = options.Value.TracksLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, "tracks"));
        }

        var artistsPath = options.Value.LocalPath;
        if (string.IsNullOrWhiteSpace(artistsPath))
        {
            throw new InvalidOperationException(
                "MusicBrainzDump:LocalPath or TracksLocalPath must be set when Source=local.");
        }

        var sibling = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
            "track.jsonl");
        return Task.FromResult(RequireExistingPath(sibling, "tracks"));
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

