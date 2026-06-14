namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.CompositionRoot;

public sealed class SearchCatalogFeatureOptions
{
    public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

    public Action<IServiceCollection>? ConfigureCatalogSearchDependencies { get; set; }
}
