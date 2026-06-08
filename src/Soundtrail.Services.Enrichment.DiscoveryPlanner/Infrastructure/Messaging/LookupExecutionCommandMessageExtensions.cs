using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model;
using Soundtrail.Domain.Commands;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static ResolveCanonicalMetadataCommandDto ToDto(this LookupMusicCommand command) =>
        new(
            CommandId.For($"ResolveCanonicalMetadata:{command.MusicCatalogId.Value}").Value,
            command.MusicCatalogId.Value,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId.Value);

    public static ResolveCanonicalMetadataCommand ToDomain(this ResolveCanonicalMetadataCommandDto dto) =>
        new(
            CommandId.From(dto.CommandId),
            MusicCatalogId.From(dto.MusicCatalogId),
            dto.Priority,
            dto.CreatedAt,
            CorrelationId.From(dto.CorrelationId));
}
