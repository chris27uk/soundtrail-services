using Raven.Client.Documents.Session;
using System.Linq;

namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels.Adapters;

public sealed class RavenClearPlannerOperationalState(
    IAsyncDocumentSession session) : IClearPlannerOperationalStatePort
{
    public async Task<ClearPlannerOperationalStateResult> ClearAsync(CancellationToken cancellationToken)
    {
        var potentialCount = await DeleteAllAsync(
            "potential-catalog-lookup-work/",
            cancellationToken);
        var trackingCount = await DeleteAllAsync(
            "catalog-search-tracking/",
            cancellationToken);
        var activeCount = await DeleteAllAsync(
            "active-lookup-work/",
            cancellationToken);

        return new ClearPlannerOperationalStateResult(potentialCount, trackingCount, activeCount);
    }

    private async Task<int> DeleteAllAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        var documents = await session.Advanced.LoadStartingWithAsync<object>(
            prefix,
            start: 0,
            pageSize: 4096);
        var loadedDocuments = documents.ToArray();

        foreach (var document in loadedDocuments)
        {
            session.Delete(document);
        }

        if (loadedDocuments.Length > 0)
        {
            await session.SaveChangesAsync(cancellationToken);
            session.Advanced.Clear();
        }

        return loadedDocuments.Length;
    }
}
