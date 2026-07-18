namespace Soundtrail.Services.Api.Features.Search.Contract
{
    public enum SearchType
    {
        Artist,
        Album,
        Track,
        All = Album | Artist | Track
    }
}
