using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseSchedulerServiceBusMessaging(builder.Configuration));
builder.Services.AddSchedulerAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
