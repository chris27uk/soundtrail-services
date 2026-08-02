using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist
{
    internal sealed class CommandBusFake : ICommandBus
    {
        private readonly Queue<IMessage> queue = [];

        public IReadOnlyCollection<IMessage> Messages => this.queue.ToArray();

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            this.queue.Enqueue(message);
            return Task.CompletedTask;
        }

        public bool TryDequeue(out IMessage message) => this.queue.TryDequeue(out message!);
    }
}
