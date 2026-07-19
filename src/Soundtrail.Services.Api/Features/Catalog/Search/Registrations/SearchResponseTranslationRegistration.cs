using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Registrations;

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
                        .ToArray(),
                    response.Discovery is null
                        ? null
                        : new DiscoveryFeedbackResponseDto(
                            response.Discovery.Status,
                            response.Discovery.Priority.ToString(),
                            response.Discovery.NextEligibleAt,
                            response.Discovery.EarliestExpectedCompletionAt,
                            response.Discovery.Reason,
                            response.Discovery.UpdatedAtUtc)),
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
                        .ToArray(),
                    dto.Discovery is null
                        ? null
                        : new DiscoveryFeedbackResponse(
                            dto.Discovery.Status,
                            Enum.Parse<LookupPriorityBand>(dto.Discovery.Priority, true),
                            dto.Discovery.NextEligibleAtUtc,
                            dto.Discovery.EarliestExpectedCompletionAtUtc,
                            dto.Discovery.Reason,
                            dto.Discovery.UpdatedAtUtc)));

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
