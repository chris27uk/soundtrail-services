namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

internal sealed class RavenMusicTrackEventSubscriptionCursorDocument
{
    public string Id { get; set; } = string.Empty;

    public string StreamId { get; set; } = string.Empty;

    public int LastPublishedVersion { get; set; }

    public static string GetDocumentId(string streamId) => $"music-track-event-cdc-cursors/{streamId}";
}
