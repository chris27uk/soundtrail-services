using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Operations;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportKworbChart;

public sealed class ImportKworbChartHandler(
    IReadKworbChartPort readKworbChartPort,
    ILoadTrackByFingerprintPort loadTrackByFingerprintPort,
    ICommandBus commandBus) : IHandler<ImportKworbChartCommand>
{
    public async Task Handle(ImportKworbChartCommand request, CancellationToken cancellationToken = default)
    {
        var trackReferences = await readKworbChartPort.ReadAsync(cancellationToken);
        var tracks = new List<TrackId>();

        foreach (var trackReference in trackReferences)
        {
            var fingerprint = TrackMatchFingerprint.FromArtistAndTitle(trackReference.ArtistName.Value, trackReference.TrackTitle);
            var trackId = await loadTrackByFingerprintPort.LoadTrackIdAsync(fingerprint, cancellationToken);

            if (trackId is not null)
            {
                tracks.Add(trackId.Value);
            }
        }

        await commandBus.SendAsync(WorldwideTopTracksPlaylistUpdated(tracks), cancellationToken);
    }

    private static PlaylistUpdated WorldwideTopTracksPlaylistUpdated(List<TrackId> tracks) => new("WorldwideSongChart", tracks.ToArray());
}
