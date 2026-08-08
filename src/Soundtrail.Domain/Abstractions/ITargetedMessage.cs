using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Abstractions;

public interface ITargetedMessage : IMessage
{
    EnrichmentTarget Target { get; }
}
