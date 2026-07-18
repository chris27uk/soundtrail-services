using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

public interface IWorkPlanner
{
    IReadOnlyList<EnrichmentTarget> Execute<T>(T input, WorkPlan plan);
}
