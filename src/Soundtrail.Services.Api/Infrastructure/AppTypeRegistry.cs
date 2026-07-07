using Soundtrail.Adapters.Registry;
using Soundtrail.Services.Api.Features.GetAlbum.Composition;

namespace Soundtrail.Services.Api
{
    public static class AppTypeRegistry
    {
        public static readonly ITypeRegistry ServiceLocation = TypeTranslationRegistry.CreateFromAssemblies(typeof(TypeTranslationRegistry).Assembly, typeof(GetAlbumFeature).Assembly);
    }
}
