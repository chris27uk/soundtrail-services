namespace Soundtrail.Translators.MusicTrackEventStore;

public interface IMusicTrackStoredEventTranslationRegistration
{
    void Register(MusicTrackStoredEventTranslationRegistry registry);
}
