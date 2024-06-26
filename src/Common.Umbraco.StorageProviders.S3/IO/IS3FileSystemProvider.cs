﻿namespace Common.Umbraco.StorageProviders.S3.IO
{
    public interface IS3FileSystemProvider
    {
        /// <summary>
        /// Get the file system by its <paramref name="name" />.
        /// </summary>
        /// <param name="name">The name of the <see cref="IS3FileSystem" />.</param>
        /// <returns>
        /// The <see cref="IS3FileSystem" />.
        /// </returns>
        IS3FileSystem GetFileSystem(string name);
    }
}