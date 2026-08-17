using System.Formats.Tar;
using SharpCompress.Compressors.Xz;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

public sealed class MusicBrainzDumpTarXzExtractor : IMusicBrainzDumpTarXzExtractor
{
    private const int StreamBufferSize = 1024 * 64;

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

        var tempPath = outputJsonlPath + ".partial";
        try
        {
            using var archiveStream = new FileStream(
                archivePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                StreamBufferSize,
                FileOptions.SequentialScan);
            using var tarSource = OpenTarSource(archiveStream);
            using var tarReader = new TarReader(tarSource, leaveOpen: true);

            while (TryGetNextEntry(tarReader) is { } entry)
            {
                if (entry.EntryType is TarEntryType.Directory
                    or TarEntryType.GlobalExtendedAttributes
                    or TarEntryType.ExtendedAttributes
                    || !MatchesEntityEntry(entry.Name, entityName))
                {
                    entry.DataStream?.CopyTo(Stream.Null);
                    continue;
                }

                using (var output = new FileStream(
                           tempPath,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None,
                           StreamBufferSize,
                           FileOptions.SequentialScan))
                {
                    if (entry.DataStream is null)
                    {
                        throw new InvalidOperationException(
                            $"MusicBrainz dump archive '{archivePath}' entry '{entry.Name}' has no data stream.");
                    }

                    entry.DataStream.CopyTo(output);
                }

                File.Move(tempPath, outputJsonlPath, overwrite: true);
                return;
            }
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }

        throw new InvalidOperationException(
            $"MusicBrainz dump archive '{archivePath}' does not contain a JSONL member for '{entityName}'.");
    }

    private static readonly byte[] XzMagic = [0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00];

    private static Stream OpenTarSource(FileStream archiveStream)
    {
        Span<byte> magic = stackalloc byte[XzMagic.Length];
        var bytesRead = archiveStream.Read(magic);
        archiveStream.Position = 0;
        if (bytesRead == XzMagic.Length && magic.SequenceEqual(XzMagic))
        {
            return new XZStream(archiveStream);
        }

        return archiveStream;
    }

    private static TarEntry? TryGetNextEntry(TarReader tarReader)
    {
        try
        {
            return tarReader.GetNextEntry(copyData: false);
        }
        catch (EndOfStreamException)
        {
            return null;
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
