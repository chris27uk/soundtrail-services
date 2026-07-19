namespace Soundtrail.Domain.Common;

public readonly record struct CorrelationId(string Value)
{
    public static CorrelationId New() => new(Guid.NewGuid().ToString("N"));

    public static CorrelationId From(string value) => new(value);

    public override string ToString() => Value;
    
    public static implicit operator string(CorrelationId correlationId) => correlationId.Value;
    
    public static implicit operator CorrelationId(string correlationId) => new(correlationId);
}
