using System.Threading.Channels;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

public sealed class ChannelImportCatalogShardWorkQueue : IImportCatalogShardWorkQueue
{
    private readonly Channel<ImportCatalogShardWork> channel =
        Channel.CreateUnbounded<ImportCatalogShardWork>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(ImportCatalogShardWork work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);
        return channel.Writer.WriteAsync(work, cancellationToken);
    }

    public IAsyncEnumerable<ImportCatalogShardWork> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
