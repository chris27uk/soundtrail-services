using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Application.Search;

public sealed record SearchMusicRequest(
    SearchQuery Query,
    Limit Limit);
