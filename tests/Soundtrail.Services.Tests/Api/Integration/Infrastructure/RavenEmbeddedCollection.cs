using Xunit;

namespace Soundtrail.Services.Tests.Api.Integration.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RavenEmbeddedCollection
{
    public const string Name = "RavenEmbedded";
}
