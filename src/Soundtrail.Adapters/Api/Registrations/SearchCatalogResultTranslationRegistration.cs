using Soundtrail.Contracts.Api;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Discovery.Candidates;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class SearchCatalogResultTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<CandidateResult, SearchCatalogResultResponseDto>(
            translate: result =>
                new SearchCatalogResultResponseDto(
                    ToResponseType(result.Type),
                    result.Id,
                    result.Name,
                    result.ArtistId,
                    result.ArtistName,
                    result.AlbumId,
                    result.AlbumName,
                    result.PlayabilityStatus.ToString(),
                    result.AvailableProviders.Select(x => x.StableValue).ToArray(),
                    result.TerminallyUnavailableProviders.Select(x => x.StableValue).ToArray(),
                    result.ProviderReferences.Select(x => registry.ToDto<ProviderReferenceResponseDto>(x)).ToArray()));
    }

    private static string ToResponseType(SearchType type) =>
        type switch
        {
            SearchType.Artist => "artist",
            SearchType.Album => "album",
            SearchType.Track => "track",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
