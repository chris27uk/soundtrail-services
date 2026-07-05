using Microsoft.AspNetCore.Builder;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseInternalProjectorServiceBusMessaging(builder.Configuration));

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
