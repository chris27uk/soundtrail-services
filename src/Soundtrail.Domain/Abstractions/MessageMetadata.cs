namespace Soundtrail.Domain.Abstractions;

public sealed record MessageMetadata(
    string? MessageId = null,
    string? CorrelationId = null,
    string? ReplyTo = null,
    string? QueueName = null,
    int RetryCount = 0,
    IReadOnlyDictionary<string, object?>? ApplicationProperties = null)
{
    public static MessageMetadata Empty { get; } = new(ApplicationProperties: new Dictionary<string, object?>());
}
