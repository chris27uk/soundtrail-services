using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Mapping;

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

        return EnsureOfficialEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.ArtistEntity,
            dumpVersion,
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

        return EnsureOfficialEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.ReleaseGroupEntity,
            dumpVersion,
            cancellationToken);
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
            return Task.FromResult(
                RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.ReleaseEntity));
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "release.jsonl");
            if (File.Exists(sibling))
            {
                return Task.FromResult(
                    RequireExistingPath(sibling, LocalMusicBrainzDumpArchiveStore.ReleaseEntity));
            }
        }

        return EnsureOfficialEntityJsonlAsync(
            LocalMusicBrainzDumpArchiveStore.ReleaseEntity,
            dumpVersion,
            cancellationToken);
    }

    public async Task<string> EnsureTracksJsonlAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value.TracksLocalPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.TrackEntity);
        }

        var artistsPath = options.Value.LocalPath;
        if (!string.IsNullOrWhiteSpace(artistsPath))
        {
            var sibling = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(artistsPath))!,
                "track.jsonl");
            if (File.Exists(sibling))
            {
                return RequireExistingPath(sibling, LocalMusicBrainzDumpArchiveStore.TrackEntity);
            }
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);
        var archiveDirectory = options.Value.ArchiveDirectory;
        if (string.IsNullOrWhiteSpace(archiveDirectory))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{LocalMusicBrainzDumpArchiveStore.TrackEntity}' from archives.");
        }

        var versionRoot = Path.Combine(Path.GetFullPath(archiveDirectory), dumpVersion.Trim());
        var trackExtractedPath = Path.Combine(versionRoot, "extracted", $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.jsonl");
        if (File.Exists(trackExtractedPath))
        {
            return trackExtractedPath;
        }

        // Denormalized track is Soundtrail-specific — use a cached archive if present, otherwise join releases.
        var trackArchivePath = Path.Combine(versionRoot, $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.tar.xz");
        var trackBlobName = MusicBrainzDumpBlobKeys.Archive(dumpVersion, LocalMusicBrainzDumpArchiveStore.TrackEntity);
        if (File.Exists(trackArchivePath) || await blobs.ExistsAsync(trackBlobName, cancellationToken))
        {
            if (!File.Exists(trackArchivePath))
            {
                await blobs.DownloadToFileAsync(trackBlobName, trackArchivePath, cancellationToken);
            }
            else if (!await blobs.ExistsAsync(trackBlobName, cancellationToken))
            {
                await blobs.UploadFromFileAsync(trackBlobName, trackArchivePath, cancellationToken);
            }

            extractor.EnsureExtracted(
                trackArchivePath,
                LocalMusicBrainzDumpArchiveStore.TrackEntity,
                trackExtractedPath);
            return trackExtractedPath;
        }

        var releasesPath = await EnsureReleasesJsonlAsync(jobId, dumpVersion, cancellationToken);
        await MusicBrainzReleaseGraphTrackJoiner.WriteJoinedTracksAsync(
            releasesPath,
            trackExtractedPath,
            cancellationToken);
        return trackExtractedPath;
    }

    private async Task<string> EnsureOfficialEntityJsonlAsync(
        string entityName,
        string dumpVersion,
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
        else
        {
            var url = BuildDownloadUrl(dumpVersion, entityName);
            await downloader.DownloadAsync(url, archivePath, cancellationToken);
            await blobs.UploadFromFileAsync(blobName, archivePath, cancellationToken);
        }

        extractor.EnsureExtracted(archivePath, entityName, extractedPath);
        return extractedPath;
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
