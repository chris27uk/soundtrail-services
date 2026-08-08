using Azure.Messaging.ServiceBus;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

internal sealed class AzureServiceBusCommandBus(
    AzureServiceBusMessageTransport transport) : ICommandBus
{
    public async Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var dto = TypeTranslationRegistry.Default.ToDto(message);
        var queueName = GetQueueName(dto);

        using var activity = MessageTelemetry.StartPublishActivity(message, dto);
        await transport.SendAsync(
            queueName,
            dto,
            new ServiceBusMessage
            {
                MessageId = message.Id.Value,
                CorrelationId = message.CorrelationId.Value
            },
            cancellationToken);
    }

    internal static string GetQueueName(object dto) =>
        dto switch
        {
            KnownMusicDataRequestedCommandDto => "known-music-data-requests",
            UnknownMusicDataRequestedCommandDto => "unknown-music-data-requests",
            AssessMusicCatalogItemCommandDto => "assess-music-catalog-item",
            DispatchLookupWorkCommandDto => "dispatch-lookup-work",
            MusicBrainzLookupCommandDto => "lookup-musicbrainz",
            StreamingLocationLookupCommandDto => "lookup-playback-references",
            PlaylistTracksLookupCommandDto => "lookup-music-playlists",
            CatalogLookupCompletedCommandDto => "catalog-lookup-completed",
            _ => throw new InvalidOperationException(
                $"No Azure Service Bus queue mapping exists for DTO type '{dto.GetType().FullName}'.")
        };
}
