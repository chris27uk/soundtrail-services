using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator.Commands;
using Soundtrail.Contracts.Worker;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static ResolveCanonicalMetadataCommandDto ToResolveCanonicalMetadataCommand(this LookupMusicCommand command) =>
        new(
            CommandId.For($"ResolveCanonicalMetadata:{command.MusicCatalogId.Value}"),
            command.MusicCatalogId,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId);
}
