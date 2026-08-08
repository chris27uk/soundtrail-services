namespace Soundtrail.Domain.Search
{
    [Flags]
    public enum SearchType
    {
        Artist = 1,
        Album = 2,
        Track = 4,
        All = Album | Artist | Track
    }
}
