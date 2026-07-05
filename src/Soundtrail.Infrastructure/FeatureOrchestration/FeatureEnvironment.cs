using System.Collections.Concurrent;

namespace Soundtrail.Adapters.FeatureOrchestration
{
    public class FeatureEnvironment : IDisposable
    {
        private readonly bool isLive;
        
        private FeatureEnvironment(bool isLive) => this.isLive = isLive;

        private static readonly ConcurrentStack<FeatureEnvironment> Environments = new();
        
        public static FeatureEnvironment Live()
        {
            var environment = new FeatureEnvironment(true);
            Environments.Push(environment);
            return environment;
        }
        
        public static bool IsProduction() => Environments.TryPeek(out var env) && env.isLive;
        
        public void Dispose()
        {
            if (Environments.Contains(this))
            {
                Environments.TryPop(out _);   
            }
        }
    }
}
