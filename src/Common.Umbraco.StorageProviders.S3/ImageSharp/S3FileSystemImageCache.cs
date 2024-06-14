using Common.Umbraco.StorageProviders.S3.IO;

using Microsoft.Extensions.Options;

using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Caching.AWS;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Common.Umbraco.StorageProviders.S3.ImageSharp
{
    public sealed class S3FileSystemImageCache : IImageCache
    {
        private readonly string _mediaFileSystemName;
        private readonly string _cachePath;
        private AWSS3StorageCache _baseCache;

        public S3FileSystemImageCache(IOptionsMonitor<S3FileSystemOptions> options, string mediaFileSystemName, string cachePath)
        {
            _mediaFileSystemName = mediaFileSystemName;
            _cachePath = cachePath;
            var fileSystemOptions = options.Get(_mediaFileSystemName);

            string bucketName = fileSystemOptions.BucketName;

            AWSS3StorageCacheOptions cacheOptions = GetAWSS3StorageCacheOptions(fileSystemOptions);

            _baseCache = new AWSS3StorageCache(Options.Create(cacheOptions));

            _ = options.OnChange(OptionsOnChange);
        }

        private void OptionsOnChange(S3FileSystemOptions options, string? name)
        {
            if (name != _mediaFileSystemName) return;

            var cacheOptions = GetAWSS3StorageCacheOptions(options);

            _baseCache = new AWSS3StorageCache(Options.Create(cacheOptions));
        }

        private AWSS3StorageCacheOptions GetAWSS3StorageCacheOptions(S3FileSystemOptions s3FileSystemOptions)
        {
            AWSS3StorageCacheOptions cacheOptions = new()
            {
                BucketName = s3FileSystemOptions.BucketName,
                Region = s3FileSystemOptions.Region,
                AccessKey = s3FileSystemOptions.AccessKey,
                AccessSecret = s3FileSystemOptions.SecretKey,
                Endpoint = s3FileSystemOptions.ServiceUrl
            };

            return cacheOptions;
        }

        public async Task<IImageCacheResolver?> GetAsync(string key)
        {
            string cacheAndKey = $"{_cachePath}/{key}";

            return await _baseCache.GetAsync(cacheAndKey);
        }

        public Task SetAsync(string key, Stream stream, ImageCacheMetadata metadata)
        {
            string cacheAndKey = $"{_cachePath}/{key}";
            return _baseCache.SetAsync(cacheAndKey, stream, metadata);
        }
    }
}
