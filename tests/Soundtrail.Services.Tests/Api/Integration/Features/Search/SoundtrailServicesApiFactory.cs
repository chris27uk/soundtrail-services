using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Features.Search;

public sealed class SoundtrailServicesApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeSearchMusicHandler SearchMusicHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<Shared.IHandler<SearchMusicRequest, SearchMusicResponse>>();
            services.AddSingleton<Shared.IHandler<SearchMusicRequest, SearchMusicResponse>>(SearchMusicHandler);
        });
    }
}
