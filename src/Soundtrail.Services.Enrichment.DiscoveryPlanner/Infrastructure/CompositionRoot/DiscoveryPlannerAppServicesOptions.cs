namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot
{
    public sealed class DiscoveryPlannerAppServicesOptions
    {
        public bool IncludeBacklogHostedService { get; set; } = true;

        public IDiscoveryPlannerDependencyProvider? DependencyProvider { get; set; }
    }
}
