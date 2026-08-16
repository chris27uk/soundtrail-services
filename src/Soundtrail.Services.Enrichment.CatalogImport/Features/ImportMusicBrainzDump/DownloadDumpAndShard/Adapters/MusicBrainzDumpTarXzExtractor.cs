using System.Diagnostics;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Compressors.Xz;
using SharpCompress.Writers.Tar;
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

    /// <summary>
    /// Creates a tiny <c>{entity}.tar.xz</c> fixture with a single <c>mbdump/{entity}</c> JSONL member.
    /// Uses the system <c>xz</c> compressor (available on macOS/Linux CI images).
    /// </summary>
    public static void CreateFixtureArchive(string archivePath, string entityName, IEnumerable<string> jsonlLines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentNullException.ThrowIfNull(jsonlLines);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivePath))!);

        var content = string.Join('\n', jsonlLines.Where(static line => !string.IsNullOrWhiteSpace(line)));
        if (content.Length > 0)
        {
            content += '\n';
        }

        var payload = Encoding.UTF8.GetBytes(content);
        var entryName = $"mbdump/{entityName}";
        var tempTarPath = archivePath + ".tar.partial";
        var tempXzPath = archivePath + ".partial";

        try
        {
            using (var tarStream = File.Create(tempTarPath))
            using (var writer = TarWriter.OpenWriter(tarStream, new TarWriterOptions(CompressionType.None, false)))
            using (var entryStream = new MemoryStream(payload))
            {
                writer.Write(entryName, entryStream, DateTime.UtcNow);
            }

            CompressWithXz(tempTarPath, tempXzPath);
            File.Move(tempXzPath, archivePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempTarPath))
            {
                File.Delete(tempTarPath);
            }

            if (File.Exists(tempXzPath))
            {
                File.Delete(tempXzPath);
            }
        }
    }

    private static void CompressWithXz(string tarPath, string xzPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "xz",
            ArgumentList = { "-k", "-f", "-c", tarPath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start xz for MusicBrainz dump fixture compression.");
        using (var output = File.Create(xzPath))
        {
            process.StandardOutput.BaseStream.CopyTo(output);
        }

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"xz failed with exit code {process.ExitCode}: {stderr}");
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
