namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public enum EnrichmentStage
{
    LocalMapping = 0,
    LocalMusicBrainzDataset = 1,
    MusicBrainzApi = 2,
    AppleMusic = 3,
    ITunesSearch = 4
}
