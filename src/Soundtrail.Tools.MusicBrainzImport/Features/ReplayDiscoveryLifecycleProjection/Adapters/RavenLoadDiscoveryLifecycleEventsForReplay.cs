using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Tools.MusicBrainzImport.Features.OnReplayCatalogSearchStatus.Adapters;

public sealed class RavenLoadDiscoveryLifecycleEventsForReplay(
    IAsyncDocumentSession session) : ILoadDiscoveryLifecycleEventsForReplayPort
{
    public async Task<IReadOnlyList<VersionedCatalogSearchDiscoveryEvent>> LoadAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria);
        var events = (await session.Advanced.LoadStartingWithAsync<DiscoveryQueryStoredEventRecordDto>(
                $"discovery-query-events/{persistentId}/"))
            .OrderBy(x => x.Version)
            .Select(x => x.ToDomainEvent())
            .ToArray();

        return events;
    }
}
