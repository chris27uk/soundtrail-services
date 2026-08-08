namespace Soundtrail.Domain.Abstractions;

public sealed record IncomingMessage<TMessage>
{
    private readonly Func<IMessage, CancellationToken, Task>? reply;

    public IncomingMessage(
        TMessage message,
        MessageMetadata metadata,
        Func<IMessage, CancellationToken, Task>? reply = null)
    {
        Message = message;
        Metadata = metadata;
        this.reply = reply;
    }

    public TMessage Message { get; }

    public MessageMetadata Metadata { get; }

    public static IncomingMessage<TMessage> Create(TMessage message)
    {
        return new IncomingMessage<TMessage>(message, MessageMetadata.Empty);
    }

    public IncomingMessage<TNextMessage> WithMessage<TNextMessage>(TNextMessage message)
    {
        return new IncomingMessage<TNextMessage>(message, Metadata, reply);
    }

    public Task ReplyAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (reply is null)
        {
            throw new InvalidOperationException("Reply is not available for this incoming message.");
        }

        return reply(message, cancellationToken);
    }
}
