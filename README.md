# Umbraco.StorageProviders.S3

S3 Blob Storage file system provider for Umbraco CMS.

Serving media files from the `/media` path with support for the image cache with files in the `/cache` path

### Usage

This provider can be added in the `Program.cs` file:

```diff
builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers()
+   .AddS3MediaFileSystemWithImageSharpCache()
    .Build();
```

Configure provider in `appsettings.json`:
```json
{
  "Umbraco": {
    "Storage": {
      "S3": {
        "Media": {
          "BucketName": "bucket-name",
          "Region": "us-easy-1",
          "ServiceURL": "url-of-s3-storage",
          "AccessKey": "access-key",
          "SecretKey": "secret-key"
        }
      }
    }
  }
}
```

## Folder structure in the AWS S3 Storage container
With an S3 bucket in place, the `media` folder will contain the traditional seen media folders and files while the `cache` folder will contain the files to support the image cache.

## License

Umbraco Storage Provider for AWS S3 is [MIT licensed](License.md).