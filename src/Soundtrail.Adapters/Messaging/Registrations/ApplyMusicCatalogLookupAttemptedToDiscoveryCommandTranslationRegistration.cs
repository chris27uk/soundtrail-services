using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class ApplyMusicCatalogLookupAttemptedToDiscoveryCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ApplyMusicCatalogLookupAttemptedToDiscoveryCommand, ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto>(
            command => new ApplyMusicCatalogLookupAttemptedToDiscoveryCommandDto(
                TypeTranslationRegistry.Default.Translate<MusicCatalogLookupAttemptedDto>(command.Attempted)));
    }
}
