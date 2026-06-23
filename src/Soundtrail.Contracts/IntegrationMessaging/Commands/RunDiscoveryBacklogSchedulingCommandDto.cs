namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record RunDiscoveryBacklogSchedulingCommandDto(DateTimeOffset Now, int Take);
