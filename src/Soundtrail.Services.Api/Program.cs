using Soundtrail.Services.Api.Features.Health;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Time;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Features.Tracks;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSingleton<IClockPort, SystemClock>();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IEnqueueMusicRequest, InMemoryEnqueueMusicRequest>();
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
    builder.Services.AddSingleton<ITrackSearchPort, RavenTrackSearchIndex>();
}

builder.Services.AddScoped<global::Soundtrail.Services.Shared.IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSearchEndpoints();

app.Run();
