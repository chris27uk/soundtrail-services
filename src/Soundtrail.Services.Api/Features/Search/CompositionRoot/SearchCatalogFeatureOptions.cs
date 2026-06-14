namespace Soundtrail.Services.Api.Features.Search.CompositionRoot;

public sealed class SearchCatalogFeatureOptions
{
    public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

    public Action<IServiceCollection>? ConfigureCatalogSearchDependencies { get; set; }
}
