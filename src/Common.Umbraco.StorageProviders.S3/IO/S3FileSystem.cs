using Amazon.S3;
using Amazon.S3.Model;

using Common.Umbraco.StorageProviders.S3.Common;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

using System.Net;

using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Extensions;

namespace Common.Umbraco.StorageProviders.S3.IO
{
    public sealed class S3FileSystem : IS3FileSystem, IFileProviderFactory
    {
        private readonly string _bucketPrefix;
        private readonly string _bucketName;
        private readonly string _rootPath;
        private readonly IIOHelper _ioHelper;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly IAmazonS3 _s3Client;
        private const string Delimiter = "/";

        public S3FileSystem(
            S3FileSystemOptions options,
            IHostingEnvironment hostingEnvironment,
            IIOHelper ioHelper,
            IContentTypeProvider contentTypeProvider,
            IAmazonS3 s3Client)
        {
            _bucketName = options.BucketName ?? throw new ArgumentNullException(nameof(options.BucketName));
            _bucketPrefix = S3FileSystemOptions.BucketPrefix;

            _rootPath = hostingEnvironment.ToAbsolute(options.VirtualPath);
            _ioHelper = ioHelper ?? throw new ArgumentNullException(nameof(ioHelper));
            _contentTypeProvider = contentTypeProvider;
            _s3Client = s3Client;
        }

        public bool CanAddPhysical => false;

        public void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            if (!overrideIfExists && FileExists(path))
            {
                throw new InvalidOperationException($"A file at path '{path}' already exists");
            }

            AddFileFromStream(path, stream);
        }

        public void AddFile(string path, string physicalPath, bool overrideIfExists = true, bool copy = false)
        {
            if (!overrideIfExists && FileExists(path))
            {
                throw new InvalidOperationException($"A file at path '{path}' already exists");
            }

            using var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read);
            AddFileFromStream(path, fileStream);
        }

        private void AddFileFromStream(string path, Stream fileStream)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = ResolveBucketPath(path),
                ContentType = ResolveContentType(path),
                InputStream = fileStream
            };

            _ = Task.Run(async () => await _s3Client.PutObjectAsync(request)).GetAwaiter().GetResult();
        }

        public void DeleteDirectory(string path)
        {
            DeleteDirectory(path, true);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = ResolveBucketPath(path, true)
            };

            ListObjectsV2Response listObjectsResponse;
            do
            {
                listObjectsResponse = Task.Run(async () => await _s3Client.ListObjectsV2Async(listObjectsRequest)).GetAwaiter().GetResult();
                listObjectsResponse.S3Objects
                        .ForEach(async obj => await _s3Client.DeleteObjectAsync(_bucketName, obj.Key));

                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            }
            while (listObjectsResponse.IsTruncated);
        }

        public void DeleteFile(string path)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = ResolveBucketPath(path)
            };
            _ = Task.Run(async () => await _s3Client.DeleteObjectAsync(deleteRequest)).GetAwaiter().GetResult();
        }

        public bool DirectoryExists(string path)
        {
            var listS3Objects = Task.Run(async () => await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = ResolveBucketPath(path, true),
                MaxKeys = 1
            })).GetAwaiter().GetResult();

            return listS3Objects.S3Objects.Count != 0;
        }

        public bool FileExists(string path)
        {
            var listS3Objects = Task.Run(async () => await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = ResolveBucketPath(path),
                MaxKeys = 1
            })).GetAwaiter().GetResult();

            return listS3Objects.S3Objects.Count != 0;
        }

        public DateTimeOffset GetCreated(string path)
        {
            //It Is Not Possible To Get Object Created Date - Bucket Versioning Required
            //Return Last Modified Date Instead
            return GetLastModified(path);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = ResolveBucketPath(path, true),
                Delimiter = Delimiter
            };

            ListObjectsV2Response listObjectsResponse = null;
            List<string> commonPrefixes = new();
            do
            {
                listObjectsResponse = Task.Run(async () => await _s3Client.ListObjectsV2Async(listObjectsRequest))
                                          .GetAwaiter()
                                          .GetResult();
                commonPrefixes.AddRange(listObjectsResponse.CommonPrefixes);

                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            }
            while (listObjectsResponse.IsTruncated);

            return commonPrefixes.Select(x => RemovePrefix(x));
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, "*.*");
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            string fileName = Path.GetFileNameWithoutExtension(filter);
            if (fileName.EndsWith("*"))
                fileName = fileName.Remove(fileName.Length - 1);

            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = ResolveBucketPath(path, true) + fileName
            };

            ListObjectsV2Response listObjectsResponse;
            List<string> listS3ObjectsKey = new();
            do
            {
                listObjectsResponse = Task.Run(async () => await _s3Client.ListObjectsV2Async(listObjectsRequest))
                                          .GetAwaiter()
                                          .GetResult();
                listS3ObjectsKey.AddRange(listObjectsResponse.S3Objects.Select(obj => obj.Key));
                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            }
            while (listObjectsResponse.IsTruncated);

            string ext = Path.GetExtension(filter);
            if (!ext.Contains("*"))
                listS3ObjectsKey = listS3ObjectsKey.Where(key => key.EndsWith(ext)).ToList();

            return listS3ObjectsKey;
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public DateTimeOffset GetLastModified(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Task.Run(async () => await _s3Client.GetObjectMetadataAsync(request)).GetAwaiter().GetResult();
            return new DateTimeOffset(response.LastModified);
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            var path = fullPathOrUrl.Replace("\\", Delimiter, StringComparison.InvariantCultureIgnoreCase);

            // if it starts with the request/URL root path, strip it and trim the starting slash to make it relative
            // eg "/Media/1234/img.jpg" => "1234/img.jpg"
            if (_ioHelper.PathStartsWith(path, _rootPath, Delimiter.ToCharArray()))
            {
                path = path[_rootPath.Length..].TrimStart(Delimiter);
            }

            // unchanged - what else?
            return path;
        }

        public long GetSize(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Task.Run(async () => await _s3Client.GetObjectMetadataAsync(request)).GetAwaiter().GetResult();
            return response.ContentLength;
        }

        public string GetUrl(string? path)
        {
            return $"/{ResolveBucketPath(path)}";
        }

        public Stream OpenFile(string path)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = ResolveBucketPath(path)
            };

            using var response = Task.Run(async () => await _s3Client.GetObjectAsync(request))
                               .GetAwaiter()
                               .GetResult();
            var stream = new MemoryStream();
            response.ResponseStream.CopyTo(stream);

            stream.Position = 0;

            return stream;
        }

        private string ResolveBucketPath(string path, bool isDir = false)
        {
            if (string.IsNullOrEmpty(path))
                return _bucketPrefix;

            path = path.Replace("\\", Delimiter, StringComparison.InvariantCultureIgnoreCase);

            if (_ioHelper.PathStartsWith(path, _rootPath, Delimiter.ToCharArray()))
            {
                // Remove request/URL root path from path (e.g. /media/abc123/file.ext to /abc123/file.ext)
                path = path[_rootPath.Length..];
                path = path.TrimStart(Delimiter.ToCharArray());
            }

            if (path.StartsWith(Delimiter))
                path = path[1..];

            //Remove Key Prefix If Duplicate
            if (path.StartsWith(_bucketPrefix, StringComparison.InvariantCultureIgnoreCase))
                path = path[_bucketPrefix.Length..];

            if (path.StartsWith(Delimiter))
                path = path[1..];

            if (isDir && !path.EndsWith(Delimiter))
                path = $"{path}{Delimiter}";

            return $"{_bucketPrefix}/{WebUtility.UrlDecode(path)}";
        }

        private string ResolveContentType(string filename)
        {
            _ = _contentTypeProvider.TryGetContentType(filename, out string contentType);
            return contentType ?? "application/octet-stream";
        }

        private string RemovePrefix(string key)
        {
            if (!string.IsNullOrEmpty(_bucketPrefix) && key.StartsWith(_bucketPrefix))
                key = key[_bucketPrefix.Length..];

            return key.Trim(Delimiter.ToCharArray());
        }

        public IFileProvider? Create() => new S3FileProvider(_s3Client, _rootPath, _bucketName, _bucketPrefix, _ioHelper);
    }
}
