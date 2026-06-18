namespace Soundtrail.Services.Api.Features.SearchCatalog.CompositionRoot;

public sealed class SearchCatalogFeatureOptions
{
    public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

    public Action<IServiceCollection>? ConfigureCatalogSearchDependencies { get; set; }
}
