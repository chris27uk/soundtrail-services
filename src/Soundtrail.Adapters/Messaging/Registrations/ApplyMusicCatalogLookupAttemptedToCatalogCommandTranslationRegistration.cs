using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Enrichment.Responses;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class ApplyMusicCatalogLookupAttemptedToCatalogCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<ApplyMusicCatalogLookupAttemptedToCatalogCommand, ApplyMusicCatalogLookupAttemptedToCatalogCommandDto>(
            command => new ApplyMusicCatalogLookupAttemptedToCatalogCommandDto(
                TypeTranslationRegistry.Default.ToDto<MusicCatalogLookupAttemptedDto>(command.Attempted)),
            dto => new ApplyMusicCatalogLookupAttemptedToCatalogCommand(
                registry.ToDomainObject<MusicCatalogLookupAttempted>(dto.Attempted)));
    }
}
