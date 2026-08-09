using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Operations;

public sealed record ImportKworbChartCommand(DateTimeOffset TriggeredAt) : IScheduledMessage;
