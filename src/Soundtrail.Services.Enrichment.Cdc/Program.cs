using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseCdcServiceBusMessaging(builder.Configuration));
builder.Services.AddCdcAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
