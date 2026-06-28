using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Adapters;

public sealed class RavenLoadPotentialCatalogLookupWork(IAsyncDocumentSession session) : ILoadPotentialCatalogLookupWorkPort
{
    public async Task<PotentialCatalogLookupWorkState> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var documentId = PotentialCatalogLookupWorkRecordDto.GetDocumentId(musicCatalogId.Value);
        var document = await session.LoadAsync<PotentialCatalogLookupWorkRecordDto>(documentId, cancellationToken);
        if (document is null)
        {
            return new PotentialCatalogLookupWorkState
            {
                MusicCatalogId = musicCatalogId.Value,
                Status = "Pending"
            };
        }

        return new PotentialCatalogLookupWorkState
        {
            MusicCatalogId = document.MusicCatalogId,
            Status = document.Status,
            RequestCount = document.RequestCount,
            HighestTrustLevelSeen = document.HighestTrustLevelSeen,
            RiskScore = document.RiskScore,
            AppliedSearchStartEventIds = [.. document.AppliedSearchStartEventIds]
        };
    }
}
