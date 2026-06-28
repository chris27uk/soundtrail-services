using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Messaging.Registrations;

public sealed class LookupStreamingLocationsCommandTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<LookupStreamingLocationsCommand, LookupStreamingLocationsCommandDto>(
            translate: command =>
                new LookupStreamingLocationsCommandDto(
                    command.CommandId.Value,
                    command.MusicCatalogId.Value,
                    command.Priority,
                    command.CreatedAt,
                    command.CorrelationId.Value,
                    new StreamingLocationSearchTermDto(
                        command.LookupKey.Kind,
                        command.LookupKey.UnifiedQuery,
                        command.LookupKey.Isrc,
                        command.LookupKey.Title,
                        command.LookupKey.Artist,
                        command.LookupKey.Album),
                    command.Hierarchy?.ArtistId?.Value,
                    command.Hierarchy?.AlbumId?.Value));
    }
}
