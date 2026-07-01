namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class CatalogProjectionCheckpointDocument
{
    public string Id { get; set; } = string.Empty;

    public string ArtistId { get; set; } = string.Empty;

    public int LastAppliedVersion { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string artistId) => $"catalog-projection-checkpoints/{artistId}";
}
