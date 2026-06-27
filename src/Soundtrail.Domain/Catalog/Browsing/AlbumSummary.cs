using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record AlbumSummary(
    AlbumId AlbumId,
    string Name,
    DateOnly? ReleaseDate,
    PlayabilityStatus PlayabilityStatus,
    IReadOnlyList<ProviderName> AvailableProviders,
    IReadOnlyList<ProviderName> TerminallyUnavailableProviders);
