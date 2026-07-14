using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Api.Features.GetTrack.Contract;

public sealed record GetTrackRequest(TrackId TrackId);
