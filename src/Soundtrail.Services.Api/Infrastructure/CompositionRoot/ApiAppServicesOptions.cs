namespace Soundtrail.Services.Api.Infrastructure.CompositionRoot
{
    public sealed class ApiAppServicesOptions
    {
        public Action<IServiceCollection>? ConfigureQueueingDependencies { get; set; }

        public Action<IServiceCollection>? ConfigureCatalogSearchDependencies { get; set; }

        public Action<IServiceCollection>? ConfigureCatalogReadDependencies { get; set; }

        public Action<IServiceCollection>? ConfigureClockDependencies { get; set; }
    }
}
