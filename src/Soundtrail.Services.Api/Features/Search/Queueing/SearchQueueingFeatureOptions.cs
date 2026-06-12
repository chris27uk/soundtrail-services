namespace Soundtrail.Services.Api.Features.Search.Queueing
{
    public sealed class SearchQueueingFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureDependencies { get; set; }
    }
}