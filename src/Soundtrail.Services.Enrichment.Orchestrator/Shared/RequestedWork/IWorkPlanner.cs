using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

public interface IWorkPlanner
{
    IReadOnlyList<EnrichmentTarget> Execute<T>(T input, WorkPlan plan);
}
