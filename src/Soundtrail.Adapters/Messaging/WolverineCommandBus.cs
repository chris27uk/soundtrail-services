using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Adapters.Registry;
using Wolverine;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Adapters.Messaging;

public sealed class WolverineCommandBus(
    IMessageBus messageBus) : ICommandBus
{
    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        object message = command switch
        {
            SearchCatalogRequested requested => TypeTranslationRegistry.Default.Translate<CatalogSearchAttemptDto>(requested),
            KnownArtistRequested requested => TypeTranslationRegistry.Default.Translate<KnownArtistRequestedDto>(requested),
            KnownAlbumRequested requested => TypeTranslationRegistry.Default.Translate<KnownAlbumRequestedDto>(requested),
            KnownTrackRequested requested => TypeTranslationRegistry.Default.Translate<KnownTrackRequestedDto>(requested),
            AssessMusicTrackCommand requested => TypeTranslationRegistry.Default.Translate<AssessMusicTrackCommandDto>(requested),
            LookupTrackMetadataCommand requested => TypeTranslationRegistry.Default.Translate<LookupTrackMetadataCommandDto>(requested),
            LookupStreamingLocationsCommand requested => TypeTranslationRegistry.Default.Translate<LookupStreamingLocationsCommandDto>(requested),
            RunDiscoveryBacklogSchedulingCommand requested => TypeTranslationRegistry.Default.Translate<RunDiscoveryBacklogSchedulingCommandDto>(requested),
            MusicCatalogLookupAttempted requested => TypeTranslationRegistry.Default.Translate<MusicCatalogLookupAttemptedDto>(requested),
            _ => command
        };

        return messageBus.SendAsync(message).AsTask();
    }
}
