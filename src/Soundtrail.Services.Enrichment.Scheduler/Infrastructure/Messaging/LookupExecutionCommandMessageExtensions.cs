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
            VerifyApplePlaybackReferenceCommand verifyApplePlaybackReferenceCommand => verifyApplePlaybackReferenceCommand.ToTransportMessage(),
            VerifyYouTubeMusicPlaybackReferenceCommand verifyYouTubeMusicPlaybackReferenceCommand => verifyYouTubeMusicPlaybackReferenceCommand.ToTransportMessage(),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown enrichment intent command.")
        };

    private static object ToTransportMessage(this ResolveCanonicalMetadataCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityResolveCanonicalMetadataCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityResolveCanonicalMetadataCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };

    private static object ToTransportMessage(this VerifyApplePlaybackReferenceCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityVerifyApplePlaybackReferenceCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityVerifyApplePlaybackReferenceCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };

    private static object ToTransportMessage(this VerifyYouTubeMusicPlaybackReferenceCommand command) =>
        command.Priority switch
        {
            LookupPriorityBand.High => new HighPriorityVerifyYouTubeMusicPlaybackReferenceCommandMessage(command),
            LookupPriorityBand.Low => new LowPriorityVerifyYouTubeMusicPlaybackReferenceCommandMessage(command),
            _ => throw new ArgumentOutOfRangeException(nameof(command.Priority), command.Priority, "Unknown lookup priority.")
        };
}
