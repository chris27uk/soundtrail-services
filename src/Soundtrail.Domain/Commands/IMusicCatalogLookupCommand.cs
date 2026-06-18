using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Commands;

public interface IMusicCatalogLookupCommand : ICommand
{
    MusicCatalogId MusicCatalogId { get; }
}
