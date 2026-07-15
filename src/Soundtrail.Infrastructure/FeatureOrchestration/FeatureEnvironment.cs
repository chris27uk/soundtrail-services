using System.Collections.Concurrent;

namespace Soundtrail.Adapters.FeatureOrchestration
{
    public static class FeatureEnvironment
    {
        private static readonly ConcurrentStack<Scope> Environments = new();

        public static IDisposable Live()
        {
            var environment = new Scope(isLive: true);
            Environments.Push(environment);
            return environment;
        }
        
        public static bool IsProduction() => Environments.TryPeek(out var env) && env.IsLive;

        private sealed class Scope(bool isLive) : IDisposable
        {
            public bool IsLive { get; } = isLive;

            public void Dispose()
            {
                if (Environments.Contains(this))
                {
                    Environments.TryPop(out _);
                }
            }
        }
    }
}
