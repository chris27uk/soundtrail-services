using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

public interface IWorkPlanner
{
    IReadOnlyList<EnrichmentTarget> Execute<T>(T input, WorkPlan plan);
}
