using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Queuing;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public static class LookupExecutionCommandMessageExtensions
{
    public static object ToResolveCanonicalMetadataTransportMessage(this LookupMusicCommand command)
    {
        var orchestrationCommand = new ResolveCanonicalMetadataCommand(
            CommandId.For($"ResolveCanonicalMetadata:{command.MusicCatalogId.Value}"),
            command.MusicCatalogId,
            command.Priority,
            command.CreatedAt,
            command.CorrelationId);

        return orchestrationCommand.ToTransportMessage();
    }

    public static object ToTransportMessage(this IEnrichmentIntentCommand command) =>
        command switch
        {
            ResolveCanonicalMetadataCommand resolveCanonicalMetadataCommand => resolveCanonicalMetadataCommand.ToTransportMessage(),
            ResolveApplePlaybackReferenceCommand resolveApplePlaybackReferenceCommand => resolveApplePlaybackReferenceCommand.ToTransportMessage(),
            ResolveYouTubeMusicPlaybackReferenceCommand resolveYouTubeMusicPlaybackReferenceCommand => resolveYouTubeMusicPlaybackReferenceCommand.ToTransportMessage(),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown enrichment intent command.")
        };

    private static object ToTransportMessage(this ResolveCanonicalMetadataCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityResolveCanonicalMetadataCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityResolveCanonicalMetadataCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };

    private static object ToTransportMessage(this ResolveApplePlaybackReferenceCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityResolveApplePlaybackReferenceCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityResolveApplePlaybackReferenceCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };

    private static object ToTransportMessage(this ResolveYouTubeMusicPlaybackReferenceCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };
}
