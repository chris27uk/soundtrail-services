using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Public.Projector.Infrastructure.CompositionRoot;
using Soundtrail.Services.Public.Projector.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UsePublicProjectorServiceBusMessaging(builder.Configuration));
builder.Services.AddPublicProjectorAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
