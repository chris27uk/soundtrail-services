namespace Soundtrail.Domain.Search
{
    public enum SearchType
    {
        Artist,
        Album,
        Track,
        All = Album | Artist | Track
    }
}
