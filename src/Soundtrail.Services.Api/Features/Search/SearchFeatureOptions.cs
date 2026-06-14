namespace Soundtrail.Services.Api.Features.Search
{
    public sealed class SearchFeatureOptions
    {
        public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

        public Action<IServiceCollection>? ConfigureCatalogSearchDependencies { get; set; }
    }
}
