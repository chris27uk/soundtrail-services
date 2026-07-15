using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;
using Soundtrail.Services.Api.Features.GetArtist.Registrations;

namespace Soundtrail.Services.Tests.Integration.Api.GetArtist;

internal sealed class GetArtistRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetArtistRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetArtistRouteTestEnvironment ForExistingArtist()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetArtistRequest, GetArtistResponse?>>(new GetArtistHandlerFake());
        var app = builder.Build();
        app.MapGetArtistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetArtistRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetArtistHandlerFake : IApiHandler<GetArtistRequest, GetArtistResponse?>
    {
        public Task<GetArtistResponse?> Handle(GetArtistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetArtistResponse?>(
                new GetArtistResponse(
                    ArtistId.From("artist-701"),
                    ArtistName.From("The Artist"),
                    null,
                    "https://cdn.soundtrail.test/artists/artist-701.jpg"));
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
