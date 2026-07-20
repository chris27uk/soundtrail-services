namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string MusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz";

    public string PlaybackReferencesLookupQueueName { get; init; } = "lookup-playback-references";

    public string MusicPlaylistLookupQueueName { get; init; } = "lookup-music-playlists";

    public string CatalogLookupCompletedQueueName { get; init; } = "catalog-lookup-completed";
}
