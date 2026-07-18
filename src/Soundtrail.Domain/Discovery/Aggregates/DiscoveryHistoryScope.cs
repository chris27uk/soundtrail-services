using Soundtrail.Domain.Abstractions.EventSourcing;
using System.Collections.Immutable;

namespace Soundtrail.Domain.Discovery.Aggregates
{
    public class DiscoveryHistoryScope : IAsyncDisposable
    {
        private static readonly AsyncLocal<ImmutableStack<DiscoveryHistoryScope>> stack = new();
        private readonly DiscoveryHistory history;
        private readonly IEventStreamRepository<CatalogWorkId> repository;
        private bool shouldSave = false;

        private DiscoveryHistoryScope(DiscoveryHistory history, IEventStreamRepository<CatalogWorkId> repository)
        {
            this.history = history;
            this.repository = repository;
        }

        public DiscoveryHistory Aggregate => this.history;

        public static async Task<DiscoveryHistoryScope> LoadFromEventStreamAsync(
            IEventStreamRepository<CatalogWorkId> repository,
            CatalogWorkId streamId,
            DiscoveryHistory.SearchRequestContext context,
            CancellationToken cancellationToken)
        {
            var (_, aggregate) = await DiscoveryHistory.LoadAsync(repository, streamId, context, cancellationToken);
            var scope = new DiscoveryHistoryScope(aggregate, repository);

            // Push the newly created scope onto the async-local stack
            var currentStack = stack.Value ?? ImmutableStack<DiscoveryHistoryScope>.Empty;
            stack.Value = currentStack.Push(scope);

            return scope;
        }

        public void Save() => this.shouldSave = true;
        
        public static DiscoveryHistory? Current => DiscoveryHistoryScope.stack.Value is { IsEmpty: false } currentStack ? currentStack.Peek().Aggregate : null;

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (this.shouldSave)
                {
                    await this.history.SaveAsync(CancellationToken.None);
                }
            }
            finally
            {
                // Pop the scope off the stack reliably during disposal
                var currentStack = stack.Value;

                if (currentStack != null && !currentStack.IsEmpty)
                {
                    stack.Value = currentStack.Pop();
                }
            }
        }
    }
}
