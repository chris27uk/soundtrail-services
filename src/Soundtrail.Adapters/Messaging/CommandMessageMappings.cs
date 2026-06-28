using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging;

public static class CommandMessageMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            SearchCatalogRequested requested => TypeTranslationRegistry.Default.Translate<CatalogSearchAttemptDto>(requested),
            KnownArtistRequested requested => TypeTranslationRegistry.Default.Translate<KnownArtistRequestedDto>(requested),
            KnownAlbumRequested requested => TypeTranslationRegistry.Default.Translate<KnownAlbumRequestedDto>(requested),
            KnownTrackRequested requested => TypeTranslationRegistry.Default.Translate<KnownTrackRequestedDto>(requested),
            AssessMusicTrackCommand assess => TypeTranslationRegistry.Default.Translate<AssessMusicTrackCommandDto>(assess),
            LookupTrackMetadataCommand musicBrainz => TypeTranslationRegistry.Default.Translate<LookupTrackMetadataCommandDto>(musicBrainz),
            LookupStreamingLocationsCommand playback => TypeTranslationRegistry.Default.Translate<LookupStreamingLocationsCommandDto>(playback),
            RunDiscoveryBacklogSchedulingCommand backlog => TypeTranslationRegistry.Default.Translate<RunDiscoveryBacklogSchedulingCommandDto>(backlog),
            ApplyMusicCatalogLookupAttemptedToCatalogCommand catalog => TypeTranslationRegistry.Default.Translate<ApplyMusicCatalogLookupAttemptedToCatalogCommandDto>(catalog),
            ApplyMusicCatalogLookupAttemptedToDiscoveryCommand discovery => TypeTranslationRegistry.Default.Translate<ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto>(discovery),
            _ => command
        };
}
