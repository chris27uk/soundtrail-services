namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot
{
    public sealed class OrchestratorAppServicesOptions
    {
        public bool IncludeBacklogHostedService { get; set; } = true;

        public IOrchestratorDependencyProvider? DependencyProvider { get; set; }
    }
}
