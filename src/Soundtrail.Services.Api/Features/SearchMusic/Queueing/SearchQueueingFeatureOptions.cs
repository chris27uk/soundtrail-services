namespace Soundtrail.Services.Api.Features.SearchMusic.Queueing
{
    public sealed class SearchQueueingFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}