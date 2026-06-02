namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;

internal sealed class RavenActiveLookupWorkDocument
{
    public string Id { get; set; } = string.Empty;

    public string MusicCatalogId { get; set; } = string.Empty;

    public string CommandId { get; set; } = string.Empty;

    public DateTimeOffset ReservedUntil { get; set; }

    public static string GetDocumentId(string musicCatalogId) =>
        $"active-lookup-work/{Uri.EscapeDataString(musicCatalogId)}";
}
