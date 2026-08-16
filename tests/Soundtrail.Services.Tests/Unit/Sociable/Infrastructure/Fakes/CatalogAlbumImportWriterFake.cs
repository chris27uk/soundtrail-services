using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class CatalogAlbumImportWriterFake : ICatalogAlbumImportWriter
{
    private readonly List<Album> imported = [];

    public IReadOnlyList<Album> Imported => imported;

    public Task WriteAsync(Album album, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(album);
        imported.Add(album);
        return Task.CompletedTask;
    }
}
