namespace Soundtrail.Contracts.EventSourcing;

public sealed class MusicTrackStoredEventRecordDto
{
    public const string AggregateTypeValue = "music-track";

    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = AggregateTypeValue;

    public int Version { get; set; }

    public string EventType { get; set; } = string.Empty;

    public TrackDiscoveredEventDataRecordDto? TrackDiscovered { get; set; }

    public ProviderReferenceDiscoveredEventDataRecordDto? ProviderReferenceDiscovered { get; set; }

    public PlaybackReferencesResolutionRequiredEventDataRecordDto? PlaybackReferencesResolutionRequired { get; set; }

    public AlbumDiscoveredEventDataRecordDto? AlbumDiscovered { get; set; }

    public ArtistDiscoveredEventDataRecordDto? ArtistDiscovered { get; set; }

    public ProviderReferenceLookupFailedEventDataRecordDto? ProviderReferenceLookupFailed { get; set; }

    public ArtworkDiscoveredEventDataRecordDto? ArtworkDiscovered { get; set; }

    public MetadataCorrectedEventDataRecordDto? MetadataCorrected { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public static string GetDocumentId(string musicCatalogId, int version) =>
        $"music-track-events/{musicCatalogId}/{version:D10}";
}
