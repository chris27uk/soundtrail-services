using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

public static class Rule
{
    public static IWorkRule On<T>(Func<T, IReadOnlyList<EnrichmentTarget>> then) => new TypedWorkRule<T>(then);

    private sealed class TypedWorkRule<T>(Func<T, IReadOnlyList<EnrichmentTarget>> then) : IWorkRule
    {
        public IReadOnlyList<EnrichmentTarget> Apply(object input) => input is T typed ? then(typed) : [];
    }
}
