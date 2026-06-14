namespace Soundtrail.Contracts.EventSourcing;

public sealed class MusicTrackEventStreamMetadataRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string AggregateType { get; set; } = MusicTrackStoredEventRecordDto.AggregateTypeValue;

    public int Version { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<string> AppliedCommandIds { get; set; } = [];

    public static string GetDocumentId(string musicCatalogId) => $"music-track-streams/{musicCatalogId}";
}
