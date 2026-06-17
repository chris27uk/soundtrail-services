namespace Soundtrail.Services.Api.Features.SearchMusic.CompositionRoot
{
    public sealed class SearchMusicFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

        public Action<IServiceCollection>? ConfigureTrackSearchDependencies { get; set; }
    }
}
