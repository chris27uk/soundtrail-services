using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class CatalogArtistImportWriterFake : ICatalogArtistImportWriter
{
    private readonly List<Artist> imported = [];

    public IReadOnlyList<Artist> Imported => imported;

    public Task WriteAsync(Artist artist, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);
        imported.Add(artist);
        return Task.CompletedTask;
    }
}
