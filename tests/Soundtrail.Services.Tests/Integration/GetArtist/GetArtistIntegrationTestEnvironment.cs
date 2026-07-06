using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetArtist;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;
using Soundtrail.Services.Api.Features.GetArtist.Registrations;

namespace Soundtrail.Services.Tests.Integration.GetArtist;

internal sealed class GetArtistIntegrationTestEnvironment : IDisposable
{
    private readonly WebApplication app;
    private readonly string routeArtistId;

    private GetArtistIntegrationTestEnvironment(WebApplication app, string routeArtistId)
    {
        this.app = app;
        this.routeArtistId = routeArtistId;
    }

    public static GetArtistIntegrationTestEnvironment ForExistingArtist(
        string artistId = "artist-701",
        string artistName = "The Artist",
        string? description = "An Artist Description",
        string? imageUrl = "https://cdn.soundtrail.test/artists/artist-701.jpg")
    {
        var response = new GetArtistResponse(
            ArtistId.From(artistId),
            ArtistName.From(artistName),
            description,
            imageUrl);

        return new GetArtistIntegrationTestEnvironment(CreateApp(new GetArtistHandlerFake(response)), artistId);
    }

    public async Task<HttpResponseMessage> GetAsync() =>
        await app.GetTestClient().GetAsync($"/artists/{routeArtistId}");

    public async Task<GetArtistResponseDto> GetPayloadAsync()
    {
        var response = await GetAsync();
        return (await response.Content.ReadFromJsonAsync<GetArtistResponseDto>())!;
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static WebApplication CreateApp(GetArtistHandlerFake handler)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetArtistRequest, GetArtistResponse?>>(handler);
        var app = builder.Build();
        app.MapGetArtistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return app;
    }

    private sealed class GetArtistHandlerFake(GetArtistResponse? response) : IApiHandler<GetArtistRequest, GetArtistResponse?>
    {
        public Task<GetArtistResponse?> Handle(GetArtistRequest request, CancellationToken cancellationToken = default) => Task.FromResult(response);
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetArtistResponse)domainObject;
            return new GetArtistResponseDto(
                response.ArtistId.Value,
                response.ArtistName.Value,
                response.Description,
                response.ImageUrl);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
