using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

internal static class ApiCommandMappings
{
    public static object ToMessage(this object command) =>
        command switch
        {
            SearchCatalogRequested requested => CatalogSearchAttemptMapper.ToDto(requested),
            KnownArtistRequested requested => KnownArtistRequestedMapper.ToDto(requested),
            KnownAlbumRequested requested => KnownAlbumRequestedMapper.ToDto(requested),
            KnownTrackRequested requested => KnownTrackRequestedMapper.ToDto(requested),
            _ => command
        };
}
