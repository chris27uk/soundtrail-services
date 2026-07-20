using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart;

public sealed class ImportKworbChartHandler(
    ICommandBus commandBus) : IHandler<ImportKworbChartCommand>
{
    public async Task Handle(ImportKworbChartCommand request, CancellationToken cancellationToken = default)
    {
        await commandBus.SendAsync(CreatePlaylistDiscoveryRequest(request), cancellationToken);
    }

    internal static RequestKnownMusicDataMessage CreatePlaylistDiscoveryRequest(ImportKworbChartCommand request)
    {
        var triggerWindowStartedAt = AlignToHour(request.TriggeredAt);
        var playlistId = PlaylistId.FromPlaylistName("WorldwideSongChart");

        return new RequestKnownMusicDataMessage(
            new CatalogItemOperation.ChildTracksForPlaylist(playlistId),
            LookupPriorityBand.High,
            100,
            0,
            triggerWindowStartedAt)
        {
            Id = MessageId.For($"kworb:{playlistId.Value}:{triggerWindowStartedAt:yyyyMMddHH}"),
            CorrelationId = CorrelationId.From($"kworb:{playlistId.Value}:{triggerWindowStartedAt:yyyyMMddHH}")
        };
    }

    private static DateTimeOffset AlignToHour(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
