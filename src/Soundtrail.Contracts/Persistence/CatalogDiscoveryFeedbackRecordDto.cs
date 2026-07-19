namespace Soundtrail.Contracts.Persistence;

public sealed class CatalogDiscoveryFeedbackRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTimeOffset? NextEligibleAtUtc { get; set; }

    public DateTimeOffset? EarliestExpectedCompletionAtUtc { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public static string GetDocumentId(string targetId) =>
        $"catalog/discovery-feedback/{Uri.EscapeDataString(targetId)}";
}
