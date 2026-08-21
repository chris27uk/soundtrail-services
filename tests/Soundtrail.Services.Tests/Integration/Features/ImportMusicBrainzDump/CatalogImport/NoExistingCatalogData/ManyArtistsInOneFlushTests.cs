using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Integration.Features.ImportMusicBrainzDump.CatalogImport.NoExistingCatalogData;

public sealed class ManyArtistsInOneFlushTests
{
    [Fact]
    public async Task When_Appending_Many_Artists_In_One_Batch_Then_No_Raven_Session_Limit_Is_Reached()
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
        var subject = new CatalogDumpBatchWriter(
            documentStore,
            TypeTranslationRegistry.Default,
            new CommandBusFake(),
            Options.Create(new MusicBrainzDumpOptions { EventAppendArtistsPerSaveChanges = 8 }),
            NullLogger<CatalogDumpBatchWriter>.Instance);
        var observedAt = DateTimeOffset.Parse("2026-08-10T00:00:00Z");
        var items = new List<CatalogDumpBatchItem>(capacity: 20);

        for (var index = 0; index < 20; index++)
        {
            items.Add(new ArtistDumpBatchItem(new Artist
            {
                Id = ArtistId.From($"many-artists-{isolation}-{index}"),
                Name = ArtistName.From($"Artist {index} {isolation}"),
                SourceSystemIds = SourceSystemIdSet.FromLegacyMusicBrainz($"mbid-{isolation}-{index}")
            }));
        }

        var act = () => subject.AppendEventsAsync(items, observedAt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
