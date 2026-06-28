using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class AlbumSummaryToAlbumSummaryResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<AlbumSummary, AlbumSummaryResponseDto>(
            translate: album =>
                new AlbumSummaryResponseDto(
                    album.AlbumId.Value,
                    album.Name,
                    album.ReleaseDate,
                    album.PlayabilityStatus.ToString(),
                    album.AvailableProviders.Select(x => x.StableValue).ToArray(),
                    album.TerminallyUnavailableProviders.Select(x => x.StableValue).ToArray()));
    }
}
