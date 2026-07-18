using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search.Registrations;

public sealed class SearchResponseTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<SearchResponse, SearchResponseDto>(
            toDto: response =>
                new SearchResponseDto(
                    response.QueryText,
                    response.Filter.ToString(),
                    response.Results.Select(
                            result => new SearchResultResponseDto(
                                result.MusicCatalogId.NormalisedIdentifier,
                                result.ResultType.ToString(),
                                result.Title,
                                result.ArtistName,
                                result.AlbumTitle,
                                result.ArtworkUrl))
                        .ToArray()),
            toDomainObject: dto =>
                new SearchResponse(
                    dto.QueryText,
                    ParseFilter(dto.Filter),
                    dto.Results.Select(
                            result => new SearchResultResponse(
                                ParseMusicCatalogId(result.MusicCatalogId, ParseFilter(result.ResultType)),
                                ParseFilter(result.ResultType),
                                result.Title,
                                result.ArtistName,
                                result.AlbumTitle,
                                result.ArtworkUrl))
                        .ToArray()));

        registry.Register<CatalogSearchRecordDto, SearchResponse>(
            translate: record =>
                new SearchResponse(
                    record.QueryText,
                    ParseFilter(record.Filter),
                    record.Results.Select(
                            result => new SearchResultResponse(
                                ParseMusicCatalogId(result.MusicCatalogId, ParseFilter(result.ResultType)),
                                ParseFilter(result.ResultType),
                                result.Title,
                                result.ArtistName,
                                result.AlbumTitle,
                                result.ArtworkUrl))
                        .ToArray()));
    }

    private static SearchType ParseFilter(string value) =>
        Enum.Parse<SearchType>(value, true);

    private static CatalogItemId ParseMusicCatalogId(string value, SearchType filter) =>
        filter switch
        {
            SearchType.Artist => new CatalogItemId.Artist(ArtistId.From(value)),
            SearchType.Album => new CatalogItemId.Album(AlbumId.From(value)),
            SearchType.Track => new CatalogItemId.Track(TrackId.From(value)),
            _ => throw new InvalidOperationException($"Unsupported search filter '{filter}'.")
        };
}
