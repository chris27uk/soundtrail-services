using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Search;

public sealed record SearchCatalogResult(
    SearchResultType Type,
    string Id,
    string Name,
    string? ArtistId,
    string? ArtistName,
    string? AlbumId,
    string? AlbumName,
    PlayabilityStatus PlayabilityStatus,
    IReadOnlyList<ProviderName> AvailableProviders,
    IReadOnlyList<ProviderName> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReference> ProviderReferences);
