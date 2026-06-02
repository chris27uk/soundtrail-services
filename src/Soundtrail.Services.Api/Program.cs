using Soundtrail.Services.Api.Features.Health;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Time;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Shared;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSingleton<IClockPort, SystemClock>();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<ILookupMusicRequestQueue, InMemoryLookupMusicRequestQueue>();
}
else
{
    builder.Host.UseWolverine(opts =>
    {
        var options = builder.Configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        opts.UseAzureServiceBus(options.ConnectionString)
            .AutoProvision()
            .SystemQueuesAreEnabled(false);

        opts.PublishMessage<LookupMusicRequest>()
            .ToAzureServiceBusQueue(options.LookupMusicRequestsQueueName);
    });

    builder.Services.AddRavenDocumentStore(builder.Configuration);
    builder.Services.AddLookupMusicRequestQueue(builder.Configuration);
    builder.Services.AddSingleton<IQueryCachePort, RavenQueryCache>();
    builder.Services.AddSingleton<ITrackSearchPort, RavenTrackSearchIndex>();
    builder.Services.AddSingleton<ICatalogLookupPort, RavenTrackLookup>();
}

builder.Services.AddSingleton<SearchMusicHandler>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSearchEndpoints();

app.Run();
