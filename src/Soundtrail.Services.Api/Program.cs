using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.Health;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Time;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

        opts.PublishMessage<LookupMusicRequestDto>()
            .ToAzureServiceBusQueue(options.LookupMusicRequestsQueueName);
    });

    builder.Services.AddRavenDocumentStore(builder.Configuration);
    builder.Services.AddLookupMusicRequestQueue(builder.Configuration);
    builder.Services.AddSingleton<ITrackSearchPort, RavenTrackSearchIndex>();
}

builder.Services.AddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapHealthEndpoints();
app.MapSearchEndpoints();

app.Run();
