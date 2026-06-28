namespace Soundtrail.Domain.Abstractions.EventSourcing;

public enum AppendOutcome
{
    Appended,
    DuplicateOperation,
    VersionMismatch
}
