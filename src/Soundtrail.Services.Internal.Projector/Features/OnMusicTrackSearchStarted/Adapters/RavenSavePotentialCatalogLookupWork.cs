using Raven.Client.Documents.Session;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateRecorded.Adapters;

public sealed class RavenSavePotentialCatalogLookupWork(IAsyncDocumentSession session) : ISavePotentialCatalogLookupWorkPort
{
    public async Task SaveAsync(PotentialCatalogLookupWorkState work, CancellationToken cancellationToken)
    {
        var documentId = PotentialCatalogLookupWorkRecordDto.GetDocumentId(work.MusicCatalogId);
        var document = await session.LoadAsync<PotentialCatalogLookupWorkRecordDto>(documentId, cancellationToken)
            ?? new PotentialCatalogLookupWorkRecordDto
            {
                Id = documentId
            };

        document.MusicCatalogId = work.MusicCatalogId;
        document.Status = work.Status;
        document.RequestCount = work.RequestCount;
        document.HighestTrustLevelSeen = work.HighestTrustLevelSeen;
        document.RiskScore = work.RiskScore;
        document.AppliedSearchStartEventIds = [.. work.AppliedSearchStartEventIds];
        await session.StoreAsync(document, cancellationToken);
    }
}
