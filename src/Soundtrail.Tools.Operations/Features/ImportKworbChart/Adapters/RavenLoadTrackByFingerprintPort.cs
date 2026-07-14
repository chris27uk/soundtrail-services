using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart.Adapters;

public sealed class RavenLoadTrackByFingerprintPort(IDocumentStore documentStore) : ILoadTrackByFingerprintPort
{
    public async Task<TrackId?> LoadTrackIdAsync(TrackMatchFingerprint fingerprint, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fingerprint.Value))
        {
            return null;
        }

        var activeSession = documentStore.OpenAsyncSession();
        var documentId = CatalogTrackMatchFingerprintRecordDto.GetDocumentId(fingerprint.Value);
        var existing = await activeSession.LoadAsync<CatalogTrackMatchFingerprintRecordDto>(documentId, cancellationToken);
        return existing is null ? null : TrackId.From(existing.TrackId);
    }
}
