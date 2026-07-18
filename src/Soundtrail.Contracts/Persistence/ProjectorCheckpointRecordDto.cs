namespace Soundtrail.Contracts.Persistence;

public sealed class ProjectorCheckpointRecordDto
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset? LastOccurredAtUtc { get; set; }

    public string? LastEventId { get; set; }
}
