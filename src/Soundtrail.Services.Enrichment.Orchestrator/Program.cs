using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseOrchestratorServiceBusMessaging(builder.Configuration));
builder.Services.AddOrchestratorAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
