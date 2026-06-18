namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string CatalogSearchAttemptsQueueName { get; init; } = "lookup-music-requests";

    public string MusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz";

    public string PlaybackReferencesLookupQueueName { get; init; } = "lookup-playback-references";

    public string EnrichmentResponsesQueueName { get; init; } = "enrichment-responses";
}
