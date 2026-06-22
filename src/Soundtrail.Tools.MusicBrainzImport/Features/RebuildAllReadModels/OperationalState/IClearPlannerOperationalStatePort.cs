namespace Soundtrail.Tools.MusicBrainzImport.Features.RebuildAllReadModels;

public interface IClearPlannerOperationalStatePort
{
    Task<ClearPlannerOperationalStateResult> ClearAsync(CancellationToken cancellationToken);
}
