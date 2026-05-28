using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Application.Search;

public sealed record SearchMusicRequest(
    SearchQuery Query,
    Limit Limit);
