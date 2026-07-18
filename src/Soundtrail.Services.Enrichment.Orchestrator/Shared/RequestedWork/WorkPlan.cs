using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

public sealed class WorkPlan
{
    private WorkPlan(IReadOnlyList<IWorkRule> rules)
    {
        Rules = rules;
    }

    public IReadOnlyList<IWorkRule> Rules { get; }

    public static WorkPlan Create(params IWorkRule[] rules) => new(rules);
}

public interface IWorkRule
{
    IReadOnlyList<EnrichmentTarget> Apply(object input);
}
