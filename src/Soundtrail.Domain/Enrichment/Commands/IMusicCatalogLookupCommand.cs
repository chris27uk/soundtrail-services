using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Domain.Commands;

public interface IMusicCatalogLookupCommand : ICommand
{
    MusicCatalogId MusicCatalogId { get; }
}
