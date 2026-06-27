using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

internal static class ApiCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            SearchCatalogRequested requested => TypeTranslationRegistry.Default.Translate<CatalogSearchAttemptDto>(requested),
            KnownArtistRequested requested => TypeTranslationRegistry.Default.Translate<KnownArtistRequestedDto>(requested),
            KnownAlbumRequested requested => TypeTranslationRegistry.Default.Translate<KnownAlbumRequestedDto>(requested),
            KnownTrackRequested requested => TypeTranslationRegistry.Default.Translate<KnownTrackRequestedDto>(requested),
            _ => command
        };
}
