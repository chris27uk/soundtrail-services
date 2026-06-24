using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record AlbumSummary(
    AlbumId AlbumId,
    string Name,
    DateOnly? ReleaseDate,
    PlayabilityStatus PlayabilityStatus,
    IReadOnlyList<ProviderName> AvailableProviders,
    IReadOnlyList<ProviderName> TerminallyUnavailableProviders);
