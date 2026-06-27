namespace Soundtrail.Adapters.MusicTrackEventStore;

public interface IMusicTrackStoredEventTranslationRegistration
{
    void Register(MusicTrackStoredEventTranslationRegistry registry);
}
