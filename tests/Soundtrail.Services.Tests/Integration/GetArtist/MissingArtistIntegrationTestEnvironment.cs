using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetArtist;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Contract;
using Soundtrail.Services.Api.Features.GetArtist.Registrations;

namespace Soundtrail.Services.Tests.Integration.GetArtist;

internal sealed class MissingArtistIntegrationTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private MissingArtistIntegrationTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public static MissingArtistIntegrationTestEnvironment ForMissingArtist()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetArtistRequest, GetArtistResponse?>>(new GetArtistHandlerFake());
        var app = builder.Build();
        app.MapGetArtistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new MissingArtistIntegrationTestEnvironment(app);
    }

    public async Task<HttpResponseMessage> GetAsync() => await app.GetTestClient().GetAsync("/artists/artist-801");

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetArtistHandlerFake : IApiHandler<GetArtistRequest, GetArtistResponse?>
    {
        public Task<GetArtistResponse?> Handle(GetArtistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetArtistResponse?>(null);
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
