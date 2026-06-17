using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

public sealed record SearchMusicRequest(NormalizedSearchQuery Query, Limit Limit, ConfidenceScore? MinConfidence = null);
