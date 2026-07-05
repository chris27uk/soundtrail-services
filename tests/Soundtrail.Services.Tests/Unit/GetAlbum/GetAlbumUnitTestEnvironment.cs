using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbum;

internal sealed class GetAlbumUnitTestEnvironment
{
    private GetAlbumUnitTestEnvironment(
        AlbumId albumId,
        GetAlbumPortFake getAlbumPortFake)
    {
        AlbumId = albumId;
        Port = getAlbumPortFake;
    }

    public AlbumId AlbumId { get; }

    public GetAlbumPortFake Port { get; }

    public static GetAlbumUnitTestEnvironment ForExistingAlbum(
        AlbumId? albumId = null,
        GetAlbumResponse? response = null) =>
        new(
            albumId ?? AlbumExistsTestData.DefaultAlbumId,
            new GetAlbumPortFake(response ?? AlbumExistsTestData.CreateResponse(albumId: albumId ?? AlbumExistsTestData.DefaultAlbumId)));

    public GetAlbumHandler CreateSubjectUnderTest() => new(Port);

    public GetAlbumRequest CreateRequest() => new(AlbumId);

    public sealed class GetAlbumPortFake(GetAlbumResponse? response) : IGetAlbumPort
    {
        public List<AlbumId> RequestedAlbumIds { get; } = [];

        public Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            RequestedAlbumIds.Add(albumId);
            return Task.FromResult(response);
        }
    }
}
