using System.Runtime.CompilerServices;
using System.Text;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;

internal static class MusicBrainzDumpShardFileReader
{
    public static async IAsyncEnumerable<MusicBrainzDumpShardLine> ReadLinesAsync(
        string path,
        long skipLines,
        long skipBytes,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        var position = 0L;
        if (skipBytes > 0)
        {
            stream.Seek(skipBytes, SeekOrigin.Begin);
            position = skipBytes;
        }

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024,
            leaveOpen: true);

        // When seeking by bytes we already stand after skipLines rows; only line-skip when bytes are unknown.
        var linesToSkip = skipBytes > 0 ? 0L : Math.Max(0L, skipLines);
        long lineNumber = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            // Shard writers use Unix '\n' terminators; count that single byte.
            position += Encoding.UTF8.GetByteCount(line) + 1;
            lineNumber++;
            if (lineNumber <= linesToSkip)
            {
                continue;
            }

            yield return new MusicBrainzDumpShardLine(line, position);
        }
    }
}
