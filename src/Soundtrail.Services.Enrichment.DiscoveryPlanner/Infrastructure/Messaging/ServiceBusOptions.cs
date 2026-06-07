namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string LookupMusicRequestsQueueName { get; init; } = "lookup-music-requests";

    public string MusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz";

    public string AppleLookupQueueName { get; init; } = "lookup-apple";

    public string YouTubeMusicLookupQueueName { get; init; } = "lookup-youtubemusic";

    public string EnrichmentResponsesQueueName { get; init; } = "enrichment-responses";
}
