namespace Soundtrail.Domain.Common;

public readonly record struct MessageId(string Value)
{
    public static MessageId New() => new(Guid.NewGuid().ToString("N"));

    public static MessageId For(string value) => new(value);

    public static MessageId From(string value) => new(value);

    public override string ToString() => Value;
    
    public static implicit operator string(MessageId commandId) => commandId.Value;
    
    public static implicit operator MessageId(string commandId) => new(commandId);
}
