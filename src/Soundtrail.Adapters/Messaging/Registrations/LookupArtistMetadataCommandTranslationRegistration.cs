using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupArtistMetadataCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<LookupArtistMetadataCommand, LookupArtistMetadataCommandDto>(
            command => new LookupArtistMetadataCommandDto(
                command.CommandId.Value,
                command.ArtistId.Value,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId.Value,
                command.ArtistName,
                command.SourceArtistId),
            dto => new LookupArtistMetadataCommand(
                CommandId.From(dto.CommandId),
                ArtistId.From(dto.ArtistId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                dto.ArtistName,
                dto.SourceArtistId));
    }
}
