using System.Threading.Channels;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

public sealed class ChannelDownloadDumpAndShardWorkQueue : IDownloadDumpAndShardWorkQueue
{
    private readonly Channel<DownloadDumpAndShardWork> channel =
        Channel.CreateUnbounded<DownloadDumpAndShardWork>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(DownloadDumpAndShardWork work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        return channel.Writer.WriteAsync(work, cancellationToken);
    }

    public IAsyncEnumerable<DownloadDumpAndShardWork> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
