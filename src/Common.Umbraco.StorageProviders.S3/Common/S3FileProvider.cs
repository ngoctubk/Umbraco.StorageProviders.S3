using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using System.Net;

using Umbraco.Cms.Core.IO;

namespace Common.Umbraco.StorageProviders.S3.Common
{
    public class S3FileProvider(IAmazonS3 s3Client, string rootPath, string bucketName, string bucketPrefix, IIOHelper ioHelper) : IFileProvider
    {
        private const string Delimiter = "/";

        private string ResolveBucketPath(string path, bool isDir = false)
        {
            if (string.IsNullOrEmpty(path))
                return bucketPrefix;

            path = path.Replace("\\", Delimiter, StringComparison.InvariantCultureIgnoreCase);

            if (ioHelper.PathStartsWith(path, rootPath, Delimiter.ToCharArray()))
            {
                // Remove request/URL root path from path (e.g. /media/abc123/file.ext to /abc123/file.ext)
                path = path[rootPath.Length..];
                path = path.TrimStart(Delimiter.ToCharArray());
            }

            if (path.StartsWith(Delimiter))
                path = path[1..];

            //Remove Key Prefix If Duplicate
            if (path.StartsWith(bucketPrefix, StringComparison.InvariantCultureIgnoreCase))
                path = path[bucketPrefix.Length..];

            if (path.StartsWith(Delimiter))
                path = path[1..];

            if (isDir && !path.EndsWith(Delimiter))
                path = $"{path}{Delimiter}";

            return $"{bucketPrefix}/{WebUtility.UrlDecode(path)}";
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {

            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = ResolveBucketPath(subpath, true)
            };

            ListObjectsV2Response listObjectsResponse;
            List<S3Object> listS3Objects = new();
            do
            {
                listObjectsResponse = Task.Run(async () => await s3Client.ListObjectsV2Async(listObjectsRequest))
                                          .GetAwaiter()
                                          .GetResult();
                listS3Objects.AddRange(listObjectsResponse.S3Objects);
                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            }
            while (listObjectsResponse.IsTruncated);

            return listS3Objects.Count == 0 ?
                    NotFoundDirectoryContents.Singleton :
                    new S3DirectoryContents(listS3Objects, s3Client, bucketName);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var listObjectsRequest = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = ResolveBucketPath(subpath)
            };
            var listObjectsResponse = Task.Run(async () => await s3Client.ListObjectsV2Async(listObjectsRequest))
                                          .GetAwaiter()
                                          .GetResult();
            return listObjectsResponse.S3Objects.Count == 0 ?
                    new NotFoundFileInfo(subpath) :
                    new S3FileInfo(listObjectsResponse.S3Objects[0], s3Client, bucketName);
        }

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
