namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public sealed class DiscoveryLifecycleProjectionCheckpointDocument
{
    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public int LastAppliedVersion { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string criteria) => $"discovery-lifecycle-projection-checkpoints/{criteria}";
}
