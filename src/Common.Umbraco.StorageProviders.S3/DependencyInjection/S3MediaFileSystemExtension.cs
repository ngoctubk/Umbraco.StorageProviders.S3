using Common.Umbraco.StorageProviders.S3.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.DependencyInjection;

namespace Common.Umbraco.StorageProviders.S3.DependencyInjection
{
    public static class S3MediaFileSystemExtension
    {
        public static IUmbracoBuilder AddS3MediaFileSystemWithImageSharpCache(this IUmbracoBuilder builder)
            => builder.AddS3MediaFileSystem()
                      .AddS3ImageSharpCache();

        public static IUmbracoBuilder AddS3MediaFileSystem(this IUmbracoBuilder builder)
            => builder.AddInternal();

        internal static IUmbracoBuilder AddInternal(this IUmbracoBuilder builder, Action<OptionsBuilder<S3FileSystemOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddInternal(S3FileSystemOptions.MediaFileSystemName, optionsBuilder =>
            {
                optionsBuilder.Configure<IOptions<GlobalSettings>>((
                    options,
                    globalSettings) => options.VirtualPath = globalSettings.Value.UmbracoMediaPath);
                configure?.Invoke(optionsBuilder);
            });

            builder.SetMediaFileSystem(provider => provider.GetRequiredService<IS3FileSystemProvider>()
                                                           .GetFileSystem(S3FileSystemOptions.MediaFileSystemName));

            return builder;
        }
    }
}
