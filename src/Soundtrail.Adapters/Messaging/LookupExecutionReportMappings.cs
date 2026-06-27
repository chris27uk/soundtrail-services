using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging;

public static class LookupExecutionReportMappings
{
    public static MusicCatalogLookupAttemptedDto ToDto(this MusicCatalogLookupAttempted attempted) =>
        TypeTranslationRegistry.Default.Translate<MusicCatalogLookupAttemptedDto>(attempted);
}
