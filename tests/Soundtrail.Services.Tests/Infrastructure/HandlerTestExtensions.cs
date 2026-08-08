using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Tests;

public static class HandlerTestExtensions
{
    public static Task Handle<TMessage>(
        this IHandler<TMessage> handler,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(message);

        return handler.Handle(
            new IncomingMessage<TMessage>(
                message,
                CreateMetadata(message)),
            cancellationToken);
    }

    private static MessageMetadata CreateMetadata<TMessage>(TMessage message)
        where TMessage : class
    {
        if (message is IMessage domainMessage)
        {
            return new MessageMetadata(
                domainMessage.Id.Value,
                domainMessage.CorrelationId.Value,
                null,
                "tests",
                0,
                new Dictionary<string, object?>());
        }

        return new MessageMetadata(
            Guid.NewGuid().ToString("N"),
            null,
            null,
            "tests",
            0,
            new Dictionary<string, object?>());
    }
}