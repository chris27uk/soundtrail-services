using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class ReadTrackForLookupPortFake : IReadTrackForLookupPort
{
    private readonly Dictionary<TrackId, TrackLookupContext> tracks = [];

    public bool SuppressWrites { get; set; }

    public ReadTrackForLookupPortFake WithTrack(TrackLookupContext track)
    {
        if (!SuppressWrites)
        {
            tracks[track.TrackId] = track;
        }

        return this;
    }

    public ReadTrackForLookupPortFake WithLookupTrack(TrackLookupContext track)
    {
        tracks[track.TrackId] = track;
        return this;
    }

    public Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken) =>
        Task.FromResult(tracks.GetValueOrDefault(trackId));
}

internal sealed class ReadStreamingLocationByProviderPortFake : IReadStreamingLocationByProviderPort
{
    private readonly Dictionary<(string Isrc, ProviderName Provider), Uri> isrcLocations = [];
    private readonly Dictionary<(string ArtistName, string TrackTitle, ProviderName Provider), Uri> metadataLocations = [];

    public ReadStreamingLocationByProviderPortFake WithIsrcLocation(string isrc, ProviderName provider, Uri url)
    {
        isrcLocations[(isrc, provider)] = url;
        return this;
    }

    public ReadStreamingLocationByProviderPortFake WithMetadataLocation(
        string artistName,
        string trackTitle,
        ProviderName provider,
        Uri url)
    {
        metadataLocations[(artistName, trackTitle, provider)] = url;
        return this;
    }

    public Task<Uri?> ReadByIsrcAsync(string isrc, ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(isrcLocations.GetValueOrDefault((isrc, provider)));

    public Task<Uri?> ReadByTrackMetadataAsync(
        string artistName,
        string trackTitle,
        ProviderName provider,
        CancellationToken cancellationToken) =>
        Task.FromResult(metadataLocations.GetValueOrDefault((artistName, trackTitle, provider)));
}

internal sealed class DiscoveryPlanningProjectionReaderFake : IDiscoveryPlanningProjectionReader
{
    private readonly Dictionary<string, DiscoveryPlanningProjection> projections = new(StringComparer.Ordinal);

    public DiscoveryPlanningProjection ProjectionToReturn { get; set; } = new(false, null, 0, 0);

    public DiscoveryPlanningProjectionReaderFake WithProjection(
        EnrichmentTarget target,
        DiscoveryPlanningProjection projection)
    {
        projections[target.NormalisedIdentifier] = projection;
        return this;
    }

    public Task<DiscoveryPlanningProjection> ReadAsync(EnrichmentTarget target, CancellationToken cancellationToken) =>
        Task.FromResult(projections.GetValueOrDefault(target.NormalisedIdentifier, ProjectionToReturn));
}
