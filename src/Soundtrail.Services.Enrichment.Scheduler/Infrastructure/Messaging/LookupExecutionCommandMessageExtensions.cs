using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static ResolveCanonicalMetadataCommand ToResolveCanonicalMetadataCommand(this LookupMusicCommand command) =>
        new(
            CommandId.For($"ResolveCanonicalMetadata:{command.MusicCatalogId.Value}"),
            command.MusicCatalogId,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId);
}
