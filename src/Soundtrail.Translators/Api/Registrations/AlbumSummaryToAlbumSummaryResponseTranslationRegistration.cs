using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

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
                    album.AvailableProviders.Select(x => x.ToPersistentId()).ToArray(),
                    album.TerminallyUnavailableProviders.Select(x => x.ToPersistentId()).ToArray()));
    }
}
