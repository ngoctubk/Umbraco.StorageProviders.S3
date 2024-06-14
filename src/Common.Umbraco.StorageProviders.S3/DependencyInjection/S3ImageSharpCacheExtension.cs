using Common.Umbraco.StorageProviders.S3.ImageSharp;
using Common.Umbraco.StorageProviders.S3.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SixLabors.ImageSharp.Web.Caching;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace Common.Umbraco.StorageProviders.S3.DependencyInjection
{
    public static class S3ImageSharpCacheExtension
    {

        private const string _cachePath = "cache";

        public static IUmbracoBuilder AddS3ImageSharpCache(this IUmbracoBuilder builder)
                => builder.AddInternal(S3FileSystemOptions.MediaFileSystemName, _cachePath);

        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, string mediaFileSystemName, string? cachePath)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddUnique<IImageCache>(provider => new S3FileSystemImageCache(
                provider.GetRequiredService<IOptionsMonitor<S3FileSystemOptions>>(), mediaFileSystemName, cachePath));

            return builder;
        }
    }
}
