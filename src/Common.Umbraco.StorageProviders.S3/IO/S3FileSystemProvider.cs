using Amazon.S3;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;

using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;

namespace Common.Umbraco.StorageProviders.S3.IO
{
    public sealed class S3FileSystemProvider : IS3FileSystemProvider
    {
        private readonly ConcurrentDictionary<string, IS3FileSystem> _fileSystems = new();
        private readonly IOptionsMonitor<S3FileSystemOptions> _optionsMonitor;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IIOHelper _ioHelper;
        private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider;

        public S3FileSystemProvider(
            IOptionsMonitor<S3FileSystemOptions> optionsMonitor,
            IHostingEnvironment hostingEnvironment,
            IIOHelper ioHelper)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _hostingEnvironment = hostingEnvironment;
            _ioHelper = ioHelper;
            _fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
            _optionsMonitor.OnChange((options, name) => _fileSystems.TryRemove(name ?? Options.DefaultName, out _));
        }

        public IS3FileSystem GetFileSystem(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return _fileSystems.GetOrAdd(name, name =>
            {
                S3FileSystemOptions options = _optionsMonitor.Get(name);

                var clientConfig = new AmazonS3Config
                {
                    AuthenticationRegion = options.Region,
                    ServiceURL = options.ServiceUrl,
                    ForcePathStyle = true
                };
                var s3Client = new AmazonS3Client(
                    options.AccessKey,
                    options.SecretKey,
                    clientConfig);

                return new S3FileSystem(options, _hostingEnvironment, _ioHelper, _fileExtensionContentTypeProvider, s3Client);
            });
        }
    }
}