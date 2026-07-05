using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;
using Soundtrail.Services.Api.Features.GetAlbum.Registrations;

namespace Soundtrail.Services.Tests.Integration.GetAlbum;

internal sealed class MissingAlbumIntegrationTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private MissingAlbumIntegrationTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public static MissingAlbumIntegrationTestEnvironment ForMissingAlbum()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetAlbumRequest, GetAlbumResponse?>>(new GetAlbumHandlerFake());
        var app = builder.Build();
        app.MapGetAlbumEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new MissingAlbumIntegrationTestEnvironment(app);
    }

    public async Task<HttpResponseMessage> GetAsync() => await app.GetTestClient().GetAsync("/artists/artist-401/albums/album-701");

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetAlbumHandlerFake : IApiHandler<GetAlbumRequest, GetAlbumResponse?>
    {
        public Task<GetAlbumResponse?> Handle(GetAlbumRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetAlbumResponse?>(null);
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetAlbumResponse)domainObject;
            return new GetAlbumResponseDto(
                response.ArtistId.Value,
                response.ArtistName.Value,
                response.AlbumId.ArtistAlbumId,
                response.ReleaseDate);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
