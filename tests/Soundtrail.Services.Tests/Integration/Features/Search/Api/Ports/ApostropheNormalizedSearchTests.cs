using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Features.Search.Api.Ports;

public sealed class ApostropheNormalizedSearchTests
{
    [Theory]
    [InlineData("Lola's Theme")]
    [InlineData("Lola\u2019s Theme")]
    public async Task When_Query_Uses_Either_Apostrophe_Then_Curly_Title_Candidate_Is_Found(string query)
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var catalogItemId = TestTrackIds.Value($"track-lola-{isolation}");
        var curlyTitle = "Lola\u2019s Theme";
        var documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
        var documentId = CatalogSearchCandidateRecordDto.GetDocumentId(catalogItemId);

        try
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(
                    new CatalogSearchCandidateRecordDto
                    {
                        Id = documentId,
                        CatalogItemId = catalogItemId,
                        CandidateKind = "track",
                        SearchText = MusicIdentityText.NormalizeFreeText($"{curlyTitle} The Shapeshifters"),
                        Title = curlyTitle,
                        ArtistName = "The Shapeshifters",
                        AlbumTitle = curlyTitle,
                        UpdatedAt = DateTimeOffset.Parse("2004-07-12T00:00:00Z")
                    },
                    documentId);
                await session.SaveChangesAsync();
            }

            var port = new RavenSearchPort(documentStore);
            var response = await port.SearchAsync(
                new SearchCriteria(query, SearchType.Track),
                CancellationToken.None);

            response.Should().NotBeNull();
            response!.Results.Should().Contain(result => result.Title == curlyTitle);
        }
        finally
        {
            await EmbeddedRavenTestServer.DeleteDocumentsAsync(documentStore, [documentId]);
            await EmbeddedRavenTestServer.DisposeAsync(documentStore);
        }
    }
}
