using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Contract;

public sealed record SearchRequest(string QueryText, SearchType Filter);