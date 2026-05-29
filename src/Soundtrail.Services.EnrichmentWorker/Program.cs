using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.EnrichmentWorker.Budgets;
using Soundtrail.Services.EnrichmentWorker.Configuration;
using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Ports;
using Soundtrail.Services.EnrichmentWorker.Providers;
using Soundtrail.Services.EnrichmentWorker.Scheduling;
using Soundtrail.Services.Shared;
using Soundtrail.Services.EnrichmentWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<EnrichmentWorkerOptions>()
    .Bind(builder.Configuration.GetSection("Enrichment"))
    .ValidateOnStart();

builder.Services.AddSingleton<EnrichmentPriorityCalculator>();
builder.Services.AddSingleton<EnrichmentCandidateSelector>();
builder.Services.AddSingleton<NextStageDecider>();
builder.Services.AddSingleton<ProviderBudgetService>();
builder.Services.AddSingleton<ProviderCircuitBreaker>();
builder.Services.AddSingleton<EnrichmentScheduler>();
builder.Services.AddSingleton<EnrichmentJobProcessor>();

builder.Services.AddSingleton<IEnrichmentProvider, LocalMappingEnricher>();
builder.Services.AddSingleton<IEnrichmentProvider, LocalMusicBrainzDatasetEnricher>();
builder.Services.AddSingleton<IEnrichmentProvider, MusicBrainzApiEnricher>();
builder.Services.AddSingleton<IEnrichmentProvider, AppleMusicEnricher>();
builder.Services.AddSingleton<IEnrichmentProvider, ITunesSearchEnricher>();

builder.Services.AddSingleton<IClockPort, SystemClockAdapter>();

var host = builder.Build();

await host.RunAsync();
