namespace Soundtrail.Contracts.EventSourcing;

public interface IEventStreamMetadataRecord
{
    int Version { get; set; }

    DateTimeOffset UpdatedAtUtc { get; set; }

    List<string> AppliedOperationIds { get; }
}
