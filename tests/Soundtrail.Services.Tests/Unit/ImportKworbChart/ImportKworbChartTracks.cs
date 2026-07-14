using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

internal static class ImportKworbChartTracks
{
    public static TrackReference[] CreateChartRows(params (string ArtistName, string TrackTitle)[] rows) =>
        rows.Select(row => new TrackReference(ArtistName.From(row.ArtistName), row.TrackTitle)).ToArray();

    public static TrackMatchFingerprint Fingerprint(string artistName, string trackTitle) =>
        TrackMatchFingerprint.FromArtistAndTitle(artistName, trackTitle);
}
