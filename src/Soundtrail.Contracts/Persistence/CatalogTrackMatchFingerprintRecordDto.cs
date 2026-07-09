namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogTrackMatchFingerprintRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Fingerprint { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string fingerprint) => $"catalog/track-match-fingerprints/{fingerprint}";
}
