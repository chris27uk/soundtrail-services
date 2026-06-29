using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupAlbumMetadataCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<LookupAlbumMetadataCommand, LookupAlbumMetadataCommandDto>(
            command => new LookupAlbumMetadataCommandDto(
                command.CommandId.Value,
                command.ArtistId.Value,
                command.AlbumId.Value,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId.Value,
                command.ArtistName,
                command.AlbumTitle,
                command.SourceAlbumId,
                command.SourceArtistId),
            dto => new LookupAlbumMetadataCommand(
                CommandId.From(dto.CommandId),
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                dto.Priority,
                dto.CreatedAt,
                CorrelationId.From(dto.CorrelationId),
                dto.ArtistName,
                dto.AlbumTitle,
                dto.SourceAlbumId,
                dto.SourceArtistId));
    }
}
