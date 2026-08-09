namespace Soundtrail.Domain.Abstractions;

public interface IScheduledMessage
{
    DateTimeOffset TriggeredAt { get; }
}
