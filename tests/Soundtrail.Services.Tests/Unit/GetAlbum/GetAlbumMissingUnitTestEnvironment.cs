using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.GetAlbum;

internal sealed class GetAlbumMissingUnitTestEnvironment
{
    private GetAlbumMissingUnitTestEnvironment(
        AlbumId albumId,
        GetAlbumPortFake getAlbumPortFake)
    {
        AlbumId = albumId;
        Port = getAlbumPortFake;
    }

    public AlbumId AlbumId { get; }

    public GetAlbumPortFake Port { get; }

    public static GetAlbumMissingUnitTestEnvironment ForMissingAlbum(AlbumId? albumId = null) =>
        new(
            albumId ?? AlbumId.From("artist-201", "album-401"),
            new GetAlbumPortFake());

    public GetAlbumHandler CreateSubjectUnderTest() => new(Port);

    public GetAlbumRequest CreateRequest() => new(AlbumId);

    public sealed class GetAlbumPortFake : IGetAlbumPort
    {
        public List<AlbumId> RequestedAlbumIds { get; } = [];

        public Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
        {
            RequestedAlbumIds.Add(albumId);
            return Task.FromResult<GetAlbumResponse?>(null);
        }
    }
}
