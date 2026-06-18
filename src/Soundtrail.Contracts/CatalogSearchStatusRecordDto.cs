namespace Soundtrail.Contracts;

public sealed class CatalogSearchStatusRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string Criteria { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public bool WillBeLookedUp { get; set; }

    public int? EstimatedRetryAfterSeconds { get; set; }

    public DateTimeOffset? EarliestExpectedCompletionAt { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public static string GetDocumentId(string criteria) => $"catalog-search-status/{criteria}";
}
