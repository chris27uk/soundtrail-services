using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

internal sealed class RavenResolutionDemandDocument
{
    public string Id { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public string QueryId { get; set; } = string.Empty;

    public int Count { get; set; }

    public static string GetDocumentId(NormalizedSearchQuery query) =>
        $"resolution-demands/{Uri.EscapeDataString(query.Value)}";
}
