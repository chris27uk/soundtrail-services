namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string AssessMusicCatalogItemQueueName { get; init; } = "assess-music-catalog-item";

    public string DispatchLookupWorkQueueName { get; init; } = "dispatch-lookup-work";

    public string PlaylistUpdatesQueueName { get; init; } = "playlist-updates";
}
