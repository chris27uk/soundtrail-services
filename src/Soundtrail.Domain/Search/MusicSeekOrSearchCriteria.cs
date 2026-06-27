namespace Soundtrail.Domain.Search
{
    public record MusicSeekOrSearchCriteria
    {
        private MusicSearchCriteria? SearchCriteria { get; init; }
        
        private KnownMusicCatalogId? SeekCriteria { get; init; }
        
        private MusicSeekOrSearchCriteria(MusicSearchCriteria? searchCriteria, KnownMusicCatalogId? seekCriteria)
        {
            SearchCriteria = searchCriteria;
            SeekCriteria = seekCriteria;
        }

        public async Task<T> MatchAsync<T>(Func<MusicSearchCriteria, Task<T>> onSearch, Func<KnownMusicCatalogId, Task<T>> onSeek)
        {
            if (SearchCriteria is not null)
            {
                return await onSearch(SearchCriteria);
            }

            if (SeekCriteria is not null)
            {
                return await onSeek(SeekCriteria);
            }

            throw new InvalidOperationException("A search-or-seek criteria must contain either search criteria or seek criteria.");
        }

        public T Match<T>(Func<MusicSearchCriteria, T> onSearch, Func<KnownMusicCatalogId, T> onSeek)
        {
            if (SearchCriteria is not null)
            {
                return onSearch(SearchCriteria);
            }

            if (SeekCriteria is not null)
            {
                return onSeek(SeekCriteria);
            }

            throw new InvalidOperationException("A search-or-seek criteria must contain either search criteria or seek criteria.");
        }

        public MusicSearchCriteria RequireSearchCriteria() =>
            SearchCriteria ?? throw new InvalidOperationException("This operation requires search criteria, but the request was keyed by a known music catalog id.");
        
        public static MusicSeekOrSearchCriteria FromSearch(MusicSearchCriteria searchCriteria) => new(searchCriteria, null);
        
        public static MusicSeekOrSearchCriteria FromSeek(KnownMusicCatalogId knownMusicCatalogId) => new(null, knownMusicCatalogId);
    }
}
