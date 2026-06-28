namespace Soundtrail.Adapters.EventSourcing;

public enum AppendOutcome
{
    Appended,
    DuplicateOperation,
    VersionMismatch
}
