using Soundtrail.Services.Features.Search.TrackSearch;

namespace Soundtrail.Services.Features.Search;

public sealed record SearchMusicRequest(SearchQuery Query, Limit Limit, ConfidenceScore? MinConfidence = null);
