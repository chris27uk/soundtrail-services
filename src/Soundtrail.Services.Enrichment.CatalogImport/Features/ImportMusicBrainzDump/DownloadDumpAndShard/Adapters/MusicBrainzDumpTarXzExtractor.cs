using SharpCompress.Archives.Tar;
using SharpCompress.Compressors.Xz;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class MusicBrainzDumpTarXzExtractor : IMusicBrainzDumpTarXzExtractor
{
    public void EnsureExtracted(string archivePath, string entityName, string outputJsonlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputJsonlPath);

        if (File.Exists(outputJsonlPath))
        {
            return;
        }

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                $"MusicBrainz dump archive was not found at '{archivePath}'.",
                archivePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputJsonlPath))!);

        using var archiveStream = File.OpenRead(archivePath);
        using var xzStream = new XZStream(archiveStream);
        using var tarBuffer = new MemoryStream();
        xzStream.CopyTo(tarBuffer);
        tarBuffer.Position = 0;
        using var archive = TarArchive.OpenArchive(tarBuffer);

        var entry = archive.Entries
            .Where(static e => !e.IsDirectory)
            .FirstOrDefault(e => MatchesEntityEntry(e.Key, entityName));

        if (entry is null)
        {
            throw new InvalidOperationException(
                $"MusicBrainz dump archive '{archivePath}' does not contain a JSONL member for '{entityName}'.");
        }

        var tempPath = outputJsonlPath + ".partial";
        try
        {
            using (var entryStream = entry.OpenEntryStream())
            using (var output = new FileStream(
                       tempPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            {
                entryStream.CopyTo(output);
            }

            File.Move(tempPath, outputJsonlPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private static bool MatchesEntityEntry(string? entryKey, string entityName)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return false;
        }

        var normalized = entryKey.Replace('\\', '/').TrimStart('/');
        var basename = Path.GetFileName(normalized);
        return string.Equals(normalized, $"mbdump/{entityName}", StringComparison.OrdinalIgnoreCase)
               || string.Equals(basename, entityName, StringComparison.OrdinalIgnoreCase)
               || string.Equals(basename, $"{entityName}.jsonl", StringComparison.OrdinalIgnoreCase);
    }
}
