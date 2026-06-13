using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddApiAppServices(builder.Configuration, builder.Environment);
}
else
{
    builder.Host.UseWolverine(opts =>
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
        var options = builder.Configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        var useDevelopmentEmulator = options.ConnectionString.IsDevelopmentEmulatorConnectionString();

        var transport = opts.UseAzureServiceBus(options.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (!useDevelopmentEmulator)
        {
            transport.AutoProvision();
        }

        opts.PublishMessage<LookupMusicRequestDto>()
            .ToAzureServiceBusQueue(options.LookupMusicRequestsQueueName);
    });
    builder.Services.AddApiAppServices(builder.Configuration, builder.Environment);
}

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapSearchEndpoints();

app.Run();
