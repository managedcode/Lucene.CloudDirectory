using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using J2N.Threading.Atomic;
using Lucene.Net.Store;
using ManagedCode.Storage.Azure;

namespace ManagedCode.Lucene.CloudDirectory;

public class CloudDirectory : BaseDirectory
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly AtomicInt64 m_sizeInBytes = new(0);

    public CloudDirectory(string connectionString, string container)
    {
        BlobServiceClient blobServiceClient = new(connectionString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(container);
        _blobContainerClient.CreateIfNotExists();

        SetLockFactory(new SingleInstanceLockFactory());
    }

    public override string GetLockID()
    {
        return "lucene-" + GetHashCode().ToString("x", CultureInfo.InvariantCulture);
    }

    public sealed override string[] ListAll()
    {
        EnsureOpen();
        return _blobContainerClient.GetBlobs().Select(s => s.Name).ToArray();
    }

    /// <summary>
    ///     Returns true iff the named file exists in this directory.
    /// </summary>
    [Obsolete("this method will be removed in 5.0")]
    public sealed override bool FileExists(string name)
    {
        EnsureOpen();
        return _blobContainerClient.GetBlobClient(name).Exists(); //TODO: CHECK NAME
    }

    /// <summary>
    ///     Returns the length in bytes of a file in the directory.
    /// </summary>
    /// <exception cref="IOException"> if the file does not exist </exception>
    public sealed override long FileLength(string name)
    {
        EnsureOpen();
        var stream = new BlobStream(_blobContainerClient.GetPageBlobClient(name));
        //if (!m_fileMap.TryGetValue(name, out RAMFile file) || file is null)
        //{
        //    throw new FileNotFoundException(name);
        //}
        return stream.Length;
    }

    /// <summary>
    ///     Return total size in bytes of all files in this directory. This is
    ///     currently quantized to <see cref="RAMOutputStream.BUFFER_SIZE" />.
    /// </summary>
    public long GetSizeInBytes()
    {
        EnsureOpen();
        return m_sizeInBytes;
    }

    /// <summary>
    ///     Removes an existing file in the directory.
    /// </summary>
    /// <exception cref="IOException"> if the file does not exist </exception>
    public override void DeleteFile(string name)
    {
        EnsureOpen();
        var client = _blobContainerClient.GetPageBlobClient(name);
        client.DeleteIfExists();
    }

    /// <summary>
    ///     Creates a new, empty file in the directory with the given name. Returns a stream writing this file.
    /// </summary>
    public override IndexOutput CreateOutput(string name, IOContext context)
    {
        EnsureOpen();
        return new BlobOutputStream(_blobContainerClient.GetPageBlobClient(name));
    }

    public override void Sync(ICollection<string> names)
    {
    }

    /// <summary>
    ///     Returns a stream reading an existing file.
    /// </summary>
    public override IndexInput OpenInput(string name, IOContext context)
    {
        EnsureOpen();
        return new BlobInputStream(_blobContainerClient.GetPageBlobClient(name));
    }

    /// <summary>
    ///     Closes the store to future operations, releasing associated memory.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsOpen = false;
        }
    }
}