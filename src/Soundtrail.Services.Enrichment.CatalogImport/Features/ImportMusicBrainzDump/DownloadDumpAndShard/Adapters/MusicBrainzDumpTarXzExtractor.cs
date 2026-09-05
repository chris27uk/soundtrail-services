using System.Formats.Tar;
using Joveler.Compression.XZ;
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

    public async IAsyncEnumerable<string> ReadJsonlLinesAsync(
        string archivePath,
        string entityName,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException(
                $"MusicBrainz dump archive was not found at '{archivePath}'.",
                archivePath);
        }

        await using var archiveStream = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            StreamBufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);
        await foreach (var line in ReadJsonlLinesAsync(
                           archiveStream,
                           entityName,
                           archivePath,
                           cancellationToken))
        {
            yield return line;
        }
    }

    public async IAsyncEnumerable<string> ReadJsonlLinesAsync(
        Stream archiveStream,
        string entityName,
        string sourceName,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        using var tarSource = OpenTarSource(archiveStream);
        using var tarReader = new TarReader(tarSource, leaveOpen: true);

        while (TryGetNextEntry(tarReader) is { } entry)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.EntryType is TarEntryType.Directory
                or TarEntryType.GlobalExtendedAttributes
                or TarEntryType.ExtendedAttributes
                || !MatchesEntityEntry(entry.Name, entityName))
            {
                entry.DataStream?.CopyTo(Stream.Null);
                continue;
            }

            if (entry.DataStream is null)
            {
                throw new InvalidOperationException(
                    $"MusicBrainz dump archive '{sourceName}' entry '{entry.Name}' has no data stream.");
            }

            using var reader = new StreamReader(entry.DataStream, leaveOpen: true);
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                yield return line;
            }

            yield break;
        }

        throw new InvalidOperationException(
            $"MusicBrainz dump archive '{sourceName}' does not contain a JSONL member for '{entityName}'.");
    }

    private static readonly byte[] XzMagic = [0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00];

    private static Stream OpenTarSource(Stream archiveStream)
    {
        if (archiveStream.CanSeek)
        {
            var origin = archiveStream.Position;
            var magic = new byte[XzMagic.Length];
            var bytesRead = 0;
            while (bytesRead < magic.Length)
            {
                var read = archiveStream.Read(magic, bytesRead, magic.Length - bytesRead);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            archiveStream.Position = origin;
            if (bytesRead == XzMagic.Length && magic.AsSpan().SequenceEqual(XzMagic))
            {
                return OpenXzStream(archiveStream);
            }

            return archiveStream;
        }

        var header = new byte[XzMagic.Length];
        var offset = 0;
        while (offset < header.Length)
        {
            var read = archiveStream.Read(header, offset, header.Length - offset);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        Stream combined = offset == 0
            ? archiveStream
            : new PrefixedStream(header.AsSpan(0, offset).ToArray(), archiveStream);
        if (offset == XzMagic.Length && header.AsSpan().SequenceEqual(XzMagic))
        {
            return OpenXzStream(combined);
        }

        return combined;
    }

    private static XZStream OpenXzStream(Stream archiveStream)
    {
        MusicBrainzXzNative.EnsureInitialized();
        return new XZStream(
            archiveStream,
            new XZDecompressOptions
            {
                LeaveOpen = true,
                BufferSize = 1024 * 1024
            });
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

    private sealed class PrefixedStream(byte[] prefix, Stream inner) : Stream
    {
        private int prefixOffset;
        private long position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (count <= 0)
            {
                return 0;
            }

            var copied = 0;
            if (prefixOffset < prefix.Length)
            {
                var remainingPrefix = prefix.Length - prefixOffset;
                var fromPrefix = Math.Min(count, remainingPrefix);
                Buffer.BlockCopy(prefix, prefixOffset, buffer, offset, fromPrefix);
                prefixOffset += fromPrefix;
                copied += fromPrefix;
                offset += fromPrefix;
                count -= fromPrefix;
            }

            if (count > 0)
            {
                copied += inner.Read(buffer, offset, count);
            }

            position += copied;
            return copied;
        }

        public override void Flush() => inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
