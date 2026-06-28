using Soundtrail.Contracts.IntegrationMessaging.Responses;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto(
    MusicCatalogLookupAttemptedDto Attempted);
