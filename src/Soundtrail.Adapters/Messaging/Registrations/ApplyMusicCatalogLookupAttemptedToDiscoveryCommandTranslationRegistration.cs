using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class ApplyMusicCatalogLookupAttemptedToDiscoveryCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand, ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto>(
            command => new ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto(
                TypeTranslationRegistry.Default.ToDto<MusicCatalogLookupAttemptedDto>(command.Attempted)),
            dto => new ApplyMusicCatalogLookupAttemptedToDiscoveryCommand(
                registry.ToDomainObject<MusicCatalogLookupAttempted>(dto.Attempted)));
    }
}
