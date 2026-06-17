using Soundtrail.Contracts;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.SearchMusic.TrackSearch;

namespace Soundtrail.Services.Tests.Integration.Api.Infrastructure;

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
