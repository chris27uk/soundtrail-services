using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;

public sealed record StartMusicBrainzDumpImport(
    MessageId Id,
    CorrelationId CorrelationId,
    DateTimeOffset RequestedAt,
    MusicBrainzDumpImportJobId JobId,
    string DumpVersion) : IMessage
{
    public static StartMusicBrainzDumpImport Create(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);

        return new StartMusicBrainzDumpImport(
            MessageId.For($"mb-dump-start:{jobId.Value}"),
            CorrelationId.From(jobId.Value),
            requestedAt,
            jobId,
            dumpVersion.Trim());
    }
}
