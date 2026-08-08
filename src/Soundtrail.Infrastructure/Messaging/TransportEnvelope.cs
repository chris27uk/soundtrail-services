using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

internal sealed record TransportEnvelope(
    BinaryData Body,
    MessageMetadata Metadata,
    string TransportSystem,
    Type TransportMessageType,
    int? DeliveryCount = null);
