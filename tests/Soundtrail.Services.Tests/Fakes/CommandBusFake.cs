using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Tests.Fakes;

internal sealed class CommandBusFake : ICommandBus
{
    private readonly Queue<IMessage> queue = [];
    private readonly List<IMessage> sentMessages = [];
    private readonly Exception? failure;

    private CommandBusFake(Exception? failure) => this.failure = failure;

    public CommandBusFake() : this(failure: null)
    {
    }

    public static CommandBusFake Empty() => new();

    public static CommandBusFake ThatThrows(Exception error) => new(failure: error);

    public IReadOnlyCollection<IMessage> Messages => this.queue.ToArray();

    public IReadOnlyList<IMessage> SentMessages => this.sentMessages;

    public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (failure is not null)
        {
            return Task.FromException(failure);
        }

        this.sentMessages.Add(message);
        this.queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public bool TryDequeue(out IMessage message) => this.queue.TryDequeue(out message!);
}
