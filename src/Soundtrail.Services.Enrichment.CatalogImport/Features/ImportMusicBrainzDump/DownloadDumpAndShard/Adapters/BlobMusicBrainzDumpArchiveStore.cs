using System.Runtime.CompilerServices;
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
        if (await TryGetCachedTrackJsonlPathAsync(dumpVersion, cancellationToken) is { } cachedPath)
        {
            return cachedPath;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);
        var archiveDirectory = options.Value.ArchiveDirectory;
        if (string.IsNullOrWhiteSpace(archiveDirectory))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{LocalMusicBrainzDumpArchiveStore.TrackEntity}' from archives.");
        }

        var outputPath = Path.Combine(
            Path.GetFullPath(archiveDirectory),
            dumpVersion.Trim(),
            "extracted",
            $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.jsonl");
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
        if (await TryGetCachedTrackJsonlPathAsync(dumpVersion, cancellationToken) is { } cachedPath)
        {
            await foreach (var line in File.ReadLinesAsync(cachedPath, cancellationToken))
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

    public IAsyncEnumerable<string> ReadArtistLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default) =>
        ReadOfficialEntityLinesAsync(
            dumpVersion,
            LocalMusicBrainzDumpArchiveStore.ArtistEntity,
            options.Value.LocalPath,
            siblingJsonlPath: null,
            cancellationToken);

    public IAsyncEnumerable<string> ReadReleaseGroupLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        var sibling = string.IsNullOrWhiteSpace(options.Value.LocalPath)
            ? null
            : Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(options.Value.LocalPath))!,
                "release-group.jsonl");
        return ReadOfficialEntityLinesAsync(
            dumpVersion,
            LocalMusicBrainzDumpArchiveStore.ReleaseGroupEntity,
            options.Value.ReleaseGroupsLocalPath,
            sibling,
            cancellationToken);
    }

    public IAsyncEnumerable<string> ReadReleaseLinesAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        CancellationToken cancellationToken = default)
    {
        _ = jobId;
        var sibling = string.IsNullOrWhiteSpace(options.Value.LocalPath)
            ? null
            : Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(options.Value.LocalPath))!,
                "release.jsonl");
        return ReadOfficialEntityLinesAsync(
            dumpVersion,
            LocalMusicBrainzDumpArchiveStore.ReleaseEntity,
            options.Value.ReleasesLocalPath,
            sibling,
            cancellationToken);
    }

    public bool HasCachedDenormalizedTrackSource(string dumpVersion)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.TracksLocalPath) &&
            File.Exists(Path.GetFullPath(options.Value.TracksLocalPath)))
        {
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
                return true;
            }
        }

        var archiveDirectory = options.Value.ArchiveDirectory;
        if (string.IsNullOrWhiteSpace(archiveDirectory) || string.IsNullOrWhiteSpace(dumpVersion))
        {
            return false;
        }

        var versionRoot = Path.Combine(Path.GetFullPath(archiveDirectory), dumpVersion.Trim());
        var trackExtractedPath = Path.Combine(
            versionRoot,
            "extracted",
            $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.jsonl");
        if (File.Exists(trackExtractedPath))
        {
            return true;
        }

        var trackArchivePath = Path.Combine(
            versionRoot,
            $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.tar.xz");
        return File.Exists(trackArchivePath);
    }

    private async IAsyncEnumerable<string> ReadOfficialEntityLinesAsync(
        string dumpVersion,
        string entityName,
        string? configuredJsonlPath,
        string? siblingJsonlPath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(configuredJsonlPath))
        {
            await foreach (var line in File.ReadLinesAsync(
                               RequireExistingPath(configuredJsonlPath, entityName),
                               cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        if (!string.IsNullOrWhiteSpace(siblingJsonlPath) && File.Exists(siblingJsonlPath))
        {
            await foreach (var line in File.ReadLinesAsync(siblingJsonlPath, cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);
        var archiveDirectory = options.Value.ArchiveDirectory;
        if (!string.IsNullOrWhiteSpace(archiveDirectory))
        {
            var extractedPath = Path.Combine(
                Path.GetFullPath(archiveDirectory),
                dumpVersion.Trim(),
                "extracted",
                $"{entityName}.jsonl");
            if (File.Exists(extractedPath))
            {
                await foreach (var line in File.ReadLinesAsync(extractedPath, cancellationToken))
                {
                    yield return line;
                }

                yield break;
            }
        }

        await using var archiveStream = await OpenOfficialArchiveStreamAsync(
            entityName,
            dumpVersion,
            cancellationToken);
        await foreach (var line in extractor.ReadJsonlLinesAsync(
                           archiveStream,
                           entityName,
                           entityName,
                           cancellationToken))
        {
            yield return line;
        }
    }

    private async Task<string?> TryGetCachedTrackJsonlPathAsync(
        string dumpVersion,
        CancellationToken cancellationToken)
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
            return null;
        }

        var versionRoot = Path.Combine(Path.GetFullPath(archiveDirectory), dumpVersion.Trim());
        var trackExtractedPath = Path.Combine(
            versionRoot,
            "extracted",
            $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.jsonl");
        if (File.Exists(trackExtractedPath))
        {
            return trackExtractedPath;
        }

        var trackArchivePath = Path.Combine(
            versionRoot,
            $"{LocalMusicBrainzDumpArchiveStore.TrackEntity}.tar.xz");
        var trackBlobName = MusicBrainzDumpBlobKeys.Archive(
            dumpVersion,
            LocalMusicBrainzDumpArchiveStore.TrackEntity);
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

        return null;
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
                               File.ReadLinesAsync(
                                   RequireExistingPath(configured, LocalMusicBrainzDumpArchiveStore.ReleaseEntity),
                                   cancellationToken),
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

        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);
        var archiveDirectory = options.Value.ArchiveDirectory;
        if (string.IsNullOrWhiteSpace(archiveDirectory))
        {
            throw new InvalidOperationException(
                $"MusicBrainzDump:ArchiveDirectory must be set when resolving '{LocalMusicBrainzDumpArchiveStore.ReleaseEntity}' from archives.");
        }

        var versionRoot = Path.Combine(Path.GetFullPath(archiveDirectory), dumpVersion.Trim());
        var extractedRelease = Path.Combine(
            versionRoot,
            "extracted",
            $"{LocalMusicBrainzDumpArchiveStore.ReleaseEntity}.jsonl");
        if (File.Exists(extractedRelease))
        {
            await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                               File.ReadLinesAsync(extractedRelease, cancellationToken),
                               cancellationToken))
            {
                yield return line;
            }

            yield break;
        }

        await using var archiveStream = await OpenOfficialArchiveStreamAsync(
            LocalMusicBrainzDumpArchiveStore.ReleaseEntity,
            dumpVersion,
            cancellationToken);
        await foreach (var line in MusicBrainzReleaseGraphTrackJoiner.EnumerateTrackJsonLinesAsync(
                           extractor.ReadJsonlLinesAsync(
                               archiveStream,
                               LocalMusicBrainzDumpArchiveStore.ReleaseEntity,
                               LocalMusicBrainzDumpArchiveStore.ReleaseEntity,
                               cancellationToken),
                           cancellationToken))
        {
            yield return line;
        }
    }

    private async Task<Stream> OpenOfficialArchiveStreamAsync(
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
        var archivePath = Path.Combine(versionRoot, $"{entityName}.tar.xz");
        if (File.Exists(archivePath))
        {
            var blobName = MusicBrainzDumpBlobKeys.Archive(dumpVersion, entityName);
            if (!await blobs.ExistsAsync(blobName, cancellationToken))
            {
                await blobs.UploadFromFileAsync(blobName, archivePath, cancellationToken);
            }

            return new FileStream(
                archivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan | FileOptions.Asynchronous);
        }

        var existingBlobName = MusicBrainzDumpBlobKeys.Archive(dumpVersion, entityName);
        if (await blobs.ExistsAsync(existingBlobName, cancellationToken))
        {
            return await blobs.OpenReadAsync(existingBlobName, cancellationToken);
        }

        var url = BuildDownloadUrl(dumpVersion, entityName);
        return await downloader.OpenReadAsync(url, cancellationToken);
    }

    private async Task<string> EnsureOfficialArchiveOnDiskAsync(
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
        Directory.CreateDirectory(versionRoot);
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

        return archivePath;
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
