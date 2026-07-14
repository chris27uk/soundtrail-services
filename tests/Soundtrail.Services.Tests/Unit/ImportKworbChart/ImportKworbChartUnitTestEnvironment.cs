using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Operations;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportKworbChart;
using Soundtrail.Tools.Operations.Features.ImportKworbChart;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Services.Tests.Unit.ImportKworbChart;

internal sealed class ImportKworbChartUnitTestEnvironment
{
    private ImportKworbChartUnitTestEnvironment(
        IReadOnlyList<TrackReference> chartRows,
        Dictionary<TrackMatchFingerprint, TrackId> trackIdsByFingerprint)
    {
        ReadKworbChartPort = new ReadKworbChartPortFake(chartRows);
        LoadTrackByFingerprintPort = new LoadTrackByFingerprintPortFake(trackIdsByFingerprint);
        CommandBus = new CommandBusFake();
    }

    public ReadKworbChartPortFake ReadKworbChartPort { get; }

    public LoadTrackByFingerprintPortFake LoadTrackByFingerprintPort { get; }

    public CommandBusFake CommandBus { get; }

    public static ImportKworbChartUnitTestEnvironment ForChart(
        IReadOnlyList<TrackReference>? chartRows = null,
        Dictionary<TrackMatchFingerprint, TrackId>? trackIdsByFingerprint = null) =>
        new(
            chartRows ?? ImportKworbChartTracks.CreateChartRows(("Artist 1", "Track 1")),
            trackIdsByFingerprint ?? new Dictionary<TrackMatchFingerprint, TrackId>
            {
                [ImportKworbChartTracks.Fingerprint("Artist 1", "Track 1")] = TrackId.From("track-1701")
            });

    public ImportKworbChartHandler CreateSubjectUnderTest() =>
        new(ReadKworbChartPort, LoadTrackByFingerprintPort, CommandBus);

    public ImportKworbChartCommand CreateRequest() => new();

    public sealed class ReadKworbChartPortFake(IReadOnlyList<TrackReference> chartRows) : IReadKworbChartPort
    {
        public Task<IReadOnlyList<TrackReference>> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(chartRows);
    }

    public sealed class LoadTrackByFingerprintPortFake(Dictionary<TrackMatchFingerprint, TrackId> trackIdsByFingerprint) : ILoadTrackByFingerprintPort
    {
        public List<TrackMatchFingerprint> RequestedFingerprints { get; } = [];

        public Task<TrackId?> LoadTrackIdAsync(TrackMatchFingerprint fingerprint, CancellationToken cancellationToken)
        {
            RequestedFingerprints.Add(fingerprint);
            return Task.FromResult(trackIdsByFingerprint.TryGetValue(fingerprint, out var trackId) ? (TrackId?)trackId : null);
        }
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<PlaylistUpdated> Commands { get; } = [];

        public Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add((PlaylistUpdated)command);
            return Task.CompletedTask;
        }
    }
}
