using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator.Commands;
using Soundtrail.Contracts.Worker;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static ResolveCanonicalMetadataCommand ToResolveCanonicalMetadataCommand(this LookupMusicCommand command) =>
        new(
            CommandId.For($"ResolveCanonicalMetadata:{command.MusicCatalogId}"),
            command.MusicCatalogId,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId);
}
