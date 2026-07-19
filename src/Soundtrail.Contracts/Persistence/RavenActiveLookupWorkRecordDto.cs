namespace Soundtrail.Contracts.Persistence;

public sealed class RavenActiveLookupWorkRecordDto
{
    public string Id { get; set; } = string.Empty;

    public string CommandId { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public static string GetDocumentId(string commandId) =>
        $"active-lookup-work/{Uri.EscapeDataString(commandId)}";
}
