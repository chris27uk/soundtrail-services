namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

internal sealed class RavenLookupExecutionReceiptDto
{
    public string Id { get; set; } = string.Empty;

    public string CommandId { get; set; } = string.Empty;

    public bool Completed { get; set; }

    public static string GetDocumentId(string commandId) =>
        $"lookup-execution-receipts/{Uri.EscapeDataString(commandId)}";
}
