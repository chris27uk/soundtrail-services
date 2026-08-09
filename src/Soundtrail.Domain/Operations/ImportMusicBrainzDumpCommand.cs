using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Operations;

public sealed record ImportMusicBrainzDumpCommand(DateTimeOffset TriggeredAt) : IScheduledMessage;
