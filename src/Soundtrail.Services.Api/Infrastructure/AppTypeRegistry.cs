using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Services.Api.Features.GetAlbum.Composition;

namespace Soundtrail.Services.Api.Infrastructure
{
    public static class AppTypeRegistry
    {
        public static readonly ITypeRegistry ServiceLocation = TypeTranslationRegistry.CreateFromAssemblies(typeof(TypeTranslationRegistry).Assembly, typeof(GetAlbumFeature).Assembly);
    }
}
