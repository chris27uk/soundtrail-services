using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.AppleMusic;
using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Features.MusicBrainz;
using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using Soundtrail.Services.Enrichment.Infrastructure.Scheduling;
using Soundtrail.Services.Enrichment.Shared.Configuration;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Enrichment.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<EnrichmentWorkerOptions>()
    .Bind(builder.Configuration.GetSection("Enrichment"))
    .ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EnrichmentWorkerOptions>>().Value);

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
