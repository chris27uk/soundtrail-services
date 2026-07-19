using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;

public sealed record GetTrackRequest(TrackId TrackId);
