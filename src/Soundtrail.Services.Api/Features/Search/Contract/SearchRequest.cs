namespace Soundtrail.Services.Api.Features.Search.Contract;

public sealed record SearchRequest(string QueryText, SearchType Filter);