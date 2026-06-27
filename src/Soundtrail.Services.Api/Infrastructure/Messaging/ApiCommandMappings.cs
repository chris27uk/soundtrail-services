using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Translators.Api;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

internal static class ApiCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            SearchCatalogRequested requested => ApiCommandMessageTranslator.ToDto(requested),
            KnownArtistRequested requested => ApiCommandMessageTranslator.ToDto(requested),
            KnownAlbumRequested requested => ApiCommandMessageTranslator.ToDto(requested),
            KnownTrackRequested requested => ApiCommandMessageTranslator.ToDto(requested),
            _ => command
        };
}
