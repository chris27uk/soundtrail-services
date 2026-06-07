using Soundtrail.Contracts;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Tests.Api.Integration.Infrastructure;

public sealed class ApiFakeSearchMusicHandler : IHandler<SearchMusicRequest, SearchMusicResponse>
{
    private readonly List<SearchMusicRequest> requests = [];
    private SearchMusicResponse response = SearchMusicResponse.Pending("default");

    public IReadOnlyList<SearchMusicRequest> Requests => requests;

    public void ClearRequests() => requests.Clear();

    public void RespondWith(SearchMusicResponse response) => this.response = response;

    public Task<SearchMusicResponse> Handle(
        SearchMusicRequest request,
        CancellationToken cancellationToken = default)
    {
        requests.Add(request);
        return Task.FromResult(response);
    }
}
