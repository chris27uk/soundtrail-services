using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class CatalogTrackImportWriterFake : ICatalogTrackImportWriter
{
    private readonly List<Track> imported = [];

    public IReadOnlyList<Track> Imported => imported;

    public Task WriteAsync(Track track, DateTimeOffset dumpObservedAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(track);
        imported.Add(track);
        return Task.CompletedTask;
    }
}
