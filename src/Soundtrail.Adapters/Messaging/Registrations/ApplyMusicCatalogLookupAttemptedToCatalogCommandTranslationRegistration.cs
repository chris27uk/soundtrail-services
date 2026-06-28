using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class ApplyMusicCatalogLookupAttemptedToCatalogCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<ApplyMusicCatalogLookupAttemptedToCatalogCommand, ApplyMusicCatalogLookupAttemptedToCatalogCommandDto>(
            command => new ApplyMusicCatalogLookupAttemptedToCatalogCommandDto(
                TypeTranslationRegistry.Default.Translate<MusicCatalogLookupAttemptedDto>(command.Attempted)));
    }
}
