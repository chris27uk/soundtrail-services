namespace Soundtrail.Services.Api.Features.Search.Contract
{
    public enum SearchFilter
    {
        Artist,
        Album,
        Track,
        All = Album | Artist | Track
    }
}
