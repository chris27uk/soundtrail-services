namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string AssessMusicTrackQueueName { get; init; } = "assess-music-track";

    public string MusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz";

    public string PlaybackReferencesLookupQueueName { get; init; } = "lookup-playback-references";
}
