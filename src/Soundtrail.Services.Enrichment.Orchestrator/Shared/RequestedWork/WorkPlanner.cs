using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

public sealed class WorkPlanner : IWorkPlanner
{
    public IReadOnlyList<EnrichmentTarget> Execute<T>(T input, WorkPlan plan) =>
        plan.Rules
            .SelectMany(rule => rule.Apply(input!))
            .ToArray();
}
