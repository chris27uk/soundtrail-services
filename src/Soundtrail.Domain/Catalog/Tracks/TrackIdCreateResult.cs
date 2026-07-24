using Dunet;

namespace Soundtrail.Domain.Catalog.Tracks;

[Union]
public partial record TrackIdCreateResult
{
    public partial record Success(TrackId Value);

    public partial record Failure(string Reason);
}
