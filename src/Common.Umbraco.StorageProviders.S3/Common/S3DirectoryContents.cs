using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.Extensions.FileProviders;

using System.Collections;

namespace Common.Umbraco.StorageProviders.S3.Common
{
    public class S3DirectoryContents(IReadOnlyCollection<S3Object> s3Objects, IAmazonS3 s3Client, string bucketName) : IDirectoryContents
    {
        public bool Exists { get; } = s3Objects.Count > 0;

        public IEnumerator<IFileInfo> GetEnumerator()
            => s3Objects.Select(s3 => new S3FileInfo(s3, s3Client, bucketName)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
