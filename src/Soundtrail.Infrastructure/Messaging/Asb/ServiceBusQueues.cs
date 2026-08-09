using Soundtrail.Contracts.IntegrationMessaging.Commands;

namespace Soundtrail.Adapters.Messaging.Asb;

public static class ServiceBusQueues
{
    public const string KnownMusicDataRequests = "known-music-data-requests";
    public const string UnknownMusicDataRequests = "unknown-music-data-requests";
    public const string AssessMusicCatalogItem = "assess-music-catalog-item";
    public const string DispatchLookupWork = "dispatch-lookup-work";
    public const string LookupMusicBrainz = "lookup-musicbrainz";
    public const string LookupPlaybackReferences = "lookup-playback-references";
    public const string LookupMusicPlaylists = "lookup-music-playlists";
    public const string CatalogLookupCompleted = "catalog-lookup-completed";
    public const string CatalogImportStart = "catalog-import-start";
    public const string CatalogImportShard = "catalog-import-shard";

    public static IReadOnlyList<string> All { get; } =
    [
        KnownMusicDataRequests,
        UnknownMusicDataRequests,
        AssessMusicCatalogItem,
        DispatchLookupWork,
        LookupMusicBrainz,
        LookupPlaybackReferences,
        LookupMusicPlaylists,
        CatalogLookupCompleted,
        CatalogImportStart,
        CatalogImportShard
    ];

    public static string For<TDto>() => For(typeof(TDto));

    public static string For(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);

        if (dtoType == typeof(KnownMusicDataRequestedCommandDto))
        {
            return KnownMusicDataRequests;
        }

        if (dtoType == typeof(UnknownMusicDataRequestedCommandDto))
        {
            return UnknownMusicDataRequests;
        }

        if (dtoType == typeof(AssessMusicCatalogItemCommandDto))
        {
            return AssessMusicCatalogItem;
        }

        if (dtoType == typeof(DispatchLookupWorkCommandDto))
        {
            return DispatchLookupWork;
        }

        if (dtoType == typeof(MusicBrainzLookupCommandDto))
        {
            return LookupMusicBrainz;
        }

        if (dtoType == typeof(StreamingLocationLookupCommandDto))
        {
            return LookupPlaybackReferences;
        }

        if (dtoType == typeof(PlaylistTracksLookupCommandDto))
        {
            return LookupMusicPlaylists;
        }

        if (dtoType == typeof(CatalogLookupCompletedCommandDto))
        {
            return CatalogLookupCompleted;
        }

        if (dtoType == typeof(StartMusicBrainzDumpImportCommandDto))
        {
            return CatalogImportStart;
        }

        if (dtoType == typeof(ImportMusicBrainzDumpShardCommandDto))
        {
            return CatalogImportShard;
        }

        throw new InvalidOperationException(
            $"No Azure Service Bus queue mapping exists for DTO type '{dtoType.FullName}'.");
    }
}
