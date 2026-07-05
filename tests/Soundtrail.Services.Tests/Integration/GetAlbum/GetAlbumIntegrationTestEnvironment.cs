using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;
using Soundtrail.Services.Api.Features.GetAlbum.Registrations;

namespace Soundtrail.Services.Tests.Integration.GetAlbum;

internal sealed class GetAlbumIntegrationTestEnvironment : IDisposable
{
    private readonly WebApplication app;
    private readonly string routeArtistId;
    private readonly string routeAlbumId;

    private GetAlbumIntegrationTestEnvironment(WebApplication app, string routeArtistId, string routeAlbumId)
    {
        this.app = app;
        this.routeArtistId = routeArtistId;
        this.routeAlbumId = routeAlbumId;
    }

    public static GetAlbumIntegrationTestEnvironment ForExistingAlbum(
        string artistId = "artist-301",
        string albumId = "album-501",
        string artistName = "The Artist",
        DateOnly? releaseDate = null)
    {
        var response = new GetAlbumResponse(
            ArtistId.From(artistId),
            ArtistName.From(artistName),
            AlbumId.From(artistId, albumId),
            "The Album",
            releaseDate ?? new DateOnly(2024, 6, 7));

        return new GetAlbumIntegrationTestEnvironment(CreateApp(new GetAlbumHandlerFake(response)), artistId, albumId);
    }

    public async Task<HttpResponseMessage> GetAsync() =>
        await app.GetTestClient().GetAsync($"/artists/{routeArtistId}/albums/{routeAlbumId}");

    public async Task<GetAlbumResponseDto> GetPayloadAsync()
    {
        var response = await GetAsync();
        return (await response.Content.ReadFromJsonAsync<GetAlbumResponseDto>())!;
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static WebApplication CreateApp(GetAlbumHandlerFake handler)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetAlbumRequest, GetAlbumResponse?>>(handler);
        var app = builder.Build();
        app.MapGetAlbumEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return app;
    }

    private sealed class GetAlbumHandlerFake(GetAlbumResponse? response) : IApiHandler<GetAlbumRequest, GetAlbumResponse?>
    {
        public Task<GetAlbumResponse?> Handle(GetAlbumRequest request, CancellationToken cancellationToken = default) => Task.FromResult(response);
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
