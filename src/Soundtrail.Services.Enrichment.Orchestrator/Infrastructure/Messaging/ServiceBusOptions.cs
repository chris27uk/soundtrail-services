namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string CatalogSearchAttemptsQueueName { get; init; } = "lookup-music-requests";

    public string DiscoveryBacklogSchedulingQueueName { get; init; } = "discovery-backlog-scheduling";

    public string AssessMusicTrackQueueName { get; init; } = "assess-music-track";

    public string MusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz";

    public string PlaybackReferencesLookupQueueName { get; init; } = "lookup-playback-references";

    public string EnrichmentResponsesQueueName { get; init; } = "enrichment-responses";

    public string MusicTrackEventsQueueName { get; init; } = "music-track-events";
}
