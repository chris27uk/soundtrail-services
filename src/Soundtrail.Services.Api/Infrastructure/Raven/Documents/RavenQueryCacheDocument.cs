using Soundtrail.Domain.Model;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

namespace Soundtrail.Services.Api.Infrastructure.Raven.Documents;

internal sealed class RavenQueryCacheDocument
{
    public string Id { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string? QueryId { get; set; }

    public int? RetryAfterSeconds { get; set; }

    public List<RavenSearchResultDocument> Results { get; set; } = [];

    public static string GetDocumentId(NormalizedSearchQuery query) =>
        $"query-caches/{Uri.EscapeDataString(query.Value)}";
}
