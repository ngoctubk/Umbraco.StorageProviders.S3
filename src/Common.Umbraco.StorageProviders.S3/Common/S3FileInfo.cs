using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.Extensions.FileProviders;

namespace Common.Umbraco.StorageProviders.S3.Common
{
    public class S3FileInfo : IFileInfo
    {
        private readonly S3Object _s3Object;
        private readonly IAmazonS3? _s3Client;
        private readonly string _bucketName;

        public S3FileInfo(S3Object s3Object, IAmazonS3 s3Client, string bucketName)
        {
            _s3Object = s3Object;
            _s3Client = s3Client;
            _bucketName = bucketName;
            LastModified = _s3Object.LastModified;
            Length = _s3Object.Size;
            Name = _s3Object.Key;
        }

        public bool Exists => true;

        public bool IsDirectory => false;

        public DateTimeOffset LastModified { get; set; }

        public long Length { get; set; }

        public string Name { get; set; }

        public string? PhysicalPath => null;

        public Stream CreateReadStream()
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = _s3Object.Key
            };

            using var response = Task.Run(async () => await _s3Client.GetObjectAsync(request))
                               .GetAwaiter()
                               .GetResult();

            var stream = new MemoryStream();
            response.ResponseStream.CopyTo(stream);

            stream.Position = 0;

            return stream;
        }
    }
}
