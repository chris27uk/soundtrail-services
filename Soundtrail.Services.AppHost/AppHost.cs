using Soundtrail.Services.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
AppHostComposition.Configure(builder);
builder.Build().Run();
