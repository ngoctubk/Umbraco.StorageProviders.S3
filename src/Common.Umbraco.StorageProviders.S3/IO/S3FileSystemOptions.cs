using Amazon.S3;

using System.ComponentModel.DataAnnotations;

namespace Common.Umbraco.StorageProviders.S3.IO
{
    public class S3FileSystemOptions
    {
        /// <summary>
        /// The media filesystem name.
        /// </summary>
        public const string MediaFileSystemName = "Media";

        /// <summary>
        /// The prefix for the media files name string.
        /// </summary>
        public const string BucketPrefix = "media";

        /// <summary>
        /// The region for the bucket
        /// </summary>
        public string Region { get; set; } = null!;

        /// <summary>
        /// The buckets name string.
        /// </summary>
        [Required]
        public string BucketName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the virtual path.
        /// </summary>
        [Required]
        public required string VirtualPath { get; set; }

        public S3CannedACL CannedACL { get; set; }

        public ServerSideEncryptionMethod ServerSideEncryptionMethod { get; set; }

        public string ServiceUrl { get; set; }

        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
    }
}