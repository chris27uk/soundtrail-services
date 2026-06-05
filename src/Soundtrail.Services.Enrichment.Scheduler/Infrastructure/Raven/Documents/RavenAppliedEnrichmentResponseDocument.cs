namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;

internal sealed class RavenAppliedEnrichmentResponseDocument
{
    public string Id { get; set; } = string.Empty;

    public string CommandId { get; set; } = string.Empty;

    public static string GetDocumentId(string commandId) =>
        $"applied-enrichment-responses/{Uri.EscapeDataString(commandId)}";
}
