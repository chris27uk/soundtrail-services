using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class BlobMusicBrainzDumpArchiveStore(
    IOptions<MusicBrainzDumpOptions> options,
    IMusicBrainzDumpBlobContainer blobs,
    IMusicBrainzDumpDownloader downloader,
    IMusicBrainzDumpTarXzExtractor extractor) : IMusicBrainzDumpArchiveStore
{
    public Task<string> EnsureArtistsJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        var configured = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.ArtistEntity));
        }

        return EnsureEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.ArtistEntity,
            dumpVersion,
            allowHttpDownload: true,
            cancellationToken);
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
            return Task.FromResult(
                RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.ReleaseGroupEntity));
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "release-group.jsonl");
            if (File.Exists(sibling))
            {
                return Task.FromResult(
                    RequireExistingPath(sibling, LocalMusicBrainzDumpArchiveStore.ReleaseGroupEntity));
            }
        }

        return EnsureEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.ReleaseGroupEntity,
            dumpVersion,
            allowHttpDownload: true,
            cancellationToken);
    }

    public Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;

        var configured = options.Value.TracksLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Task.FromResult(RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.TrackEntity));
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "track.jsonl");
            if (File.Exists(sibling))
            {
                return Task.FromResult(RequireExistingPath(sibling, LocalMusicBrainzDumpArchiveStore.TrackEntity));
            }
        }

        // Denormalized track-graph is Soundtrail-specific; never HTTP-download official recording dumps here.
        return EnsureEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.TrackEntity,
            dumpVersion,
            allowHttpDownload: false,
            cancellationToken);
    }

    private async Task<string> EnsureEntityJsonlAsync(
        string entityName,
        string dumpVersion,
        bool allowHttpDownload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);

        var archiveDirectory = options.Value.ArchiveDirectory;
        if (string.IsNullOrWhiteSpace(archiveDirectory))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{entityName}' from archives.");
        }

        var versionRoot = Path.Combine(Path.GetFullPath(archiveDirectory), dumpVersion.Trim());
        var extractedPath = Path.Combine(versionRoot, "extracted", $"{entityName}.jsonl");
        if (File.Exists(extractedPath))
        {
            return extractedPath;
        }

        var archivePath = Path.Combine(versionRoot, $"{entityName}.tar.xz");
        var blobName = MusicBrainzDumpBlobKeys.Archive(dumpVersion, entityName);

        if (await blobs.ExistsAsync(blobName, cancellationToken))
        {
            if (!File.Exists(archivePath))
            {
                await blobs.DownloadToFileAsync(blobName, archivePath, cancellationToken);
            }
        }
        else if (File.Exists(archivePath))
        {
            await blobs.UploadFromFileAsync(blobName, archivePath, cancellationToken);
        }
        else if (allowHttpDownload && IsHttpSource())
        {
            var url = BuildDownloadUrl(dumpVersion, entityName);
            await downloader.DownloadAsync(url, archivePath, cancellationToken);
            await blobs.UploadFromFileAsync(blobName, archivePath, cancellationToken);
        }
        else
        {
            throw new FileNotFoundException(
                $"MusicBrainz {entityName} archive was not found in blob '{blobName}' or at '{archivePath}'.",
                archivePath);
        }

        extractor.EnsureExtracted(archivePath, entityName, extractedPath);
        return extractedPath;
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
