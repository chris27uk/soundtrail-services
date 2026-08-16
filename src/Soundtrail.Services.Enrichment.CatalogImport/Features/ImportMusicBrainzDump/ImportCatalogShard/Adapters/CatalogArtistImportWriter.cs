using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;

public sealed class CatalogArtistImportWriter(
    IEventStreamRepository<ArtistId> artistRepository,
    IDocumentStore documentStore) : ICatalogArtistImportWriter
{
    public async Task WriteAsync(
        Artist artist,
        DateTimeOffset dumpObservedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artist);

        var (stream, catalog) = await ArtistCatalog.LoadAsync(artistRepository, artist.Id, cancellationToken);
        var existing = stream.Events.OfType<ArtistDiscovered>().LastOrDefault();
        if (existing is not null && existing.ObservedAt >= dumpObservedAt)
        {
            return;
        }

        catalog.CatalogItemDiscovered(new CatalogItem.MusicArtist(artist));
        await catalog.SaveAsync(
            artistRepository,
            stream,
            MessageId.For($"bulk-import:ArtistDiscovered:{artist.Id.Value}:{dumpObservedAt:O}"),
            cancellationToken,
            ProjectionHint.BulkImport);

        await StoreArtistReadModelAsync(artist, dumpObservedAt, cancellationToken);
    }

    private async Task StoreArtistReadModelAsync(
        Artist artist,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(artist.Id.Value),
                ArtistId = artist.Id.Value,
                Name = artist.Name.Value,
                NormalizedName = MusicIdentityText.NormalizeFreeText(artist.Name.Value),
                SearchText = artist.Name.Value,
                MusicBrainzArtistId = SourceSystemIdSet.MusicBrainzIdOrNull(artist.SourceSystemIds),
                AvailableProviders = [],
                TerminallyUnavailableProviders = [],
                ArtworkUrl = artist.ImageUrl,
                UpdatedAt = updatedAt
            },
            cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
