using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Search;

public sealed class SoundtrailServicesApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeCatalogLookupPort CatalogLookup { get; } = new();

    public ApiFakeTrackSearchPort TrackSearch { get; } = new();

    public ApiFakeEnqueueMusicRequest EnqueueMusicRequests { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICatalogLookupPort>();
            services.RemoveAll<ITrackSearchPort>();
            services.RemoveAll<IEnqueueMusicRequest>();
            services.AddSingleton<ICatalogLookupPort>(CatalogLookup);
            services.AddSingleton<ITrackSearchPort>(TrackSearch);
            services.AddSingleton<IEnqueueMusicRequest>(EnqueueMusicRequests);
        });
    }
}
