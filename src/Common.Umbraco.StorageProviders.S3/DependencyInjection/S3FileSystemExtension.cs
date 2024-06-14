using Common.Umbraco.StorageProviders.S3.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.DependencyInjection;

namespace Common.Umbraco.StorageProviders.S3.DependencyInjection
{
    public static class S3FileSystemExtension
    {
        internal static IUmbracoBuilder AddInternal(
            this IUmbracoBuilder builder,
            string name,
            Action<OptionsBuilder<S3FileSystemOptions>>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(name);

            builder.Services.TryAddSingleton<IS3FileSystemProvider, S3FileSystemProvider>();

            OptionsBuilder<S3FileSystemOptions> optionsBuilder = builder.Services.AddOptions<S3FileSystemOptions>(name)
                .BindConfiguration($"Umbraco:Storage:S3:{name}")
                .ValidateDataAnnotations();

            configure?.Invoke(optionsBuilder);

            return builder;
        }
    }
}
