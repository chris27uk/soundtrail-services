namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot
{
    public sealed class WorkerAppServicesOptions
    {
        public IWorkerDependencyProvider? DependencyProvider { get; set; }
    }
}