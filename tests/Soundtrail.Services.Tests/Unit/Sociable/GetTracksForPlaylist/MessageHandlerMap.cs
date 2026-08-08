using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed class MessageHandlerMap
{
    private readonly Dictionary<Type, Func<IMessage, CancellationToken, Task>> handlers = [];

    public MessageHandlerMap Add<TMessage>(IHandler<TMessage> handler)
        where TMessage : IMessage
    {
        handlers.Add(
            typeof(TMessage),
            (message, cancellationToken) =>
                handler.Handle(IncomingMessage<TMessage>.Create((TMessage)message), cancellationToken));
        return this;
    }

    public Task HandleAsync(IMessage message, CancellationToken cancellationToken) =>
        handlers.TryGetValue(message.GetType(), out var handler)
            ? handler(message, cancellationToken)
            : throw new InvalidOperationException($"No sociable handler is mapped for '{message.GetType().Name}'.");
}
