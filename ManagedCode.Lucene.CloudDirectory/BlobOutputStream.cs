using System.IO;
using Azure.Storage.Blobs.Specialized;
using Lucene.Net.Store;
using ManagedCode.Storage.Azure;

namespace ManagedCode.Lucene.CloudDirectory;

public class BlobOutputStream : IndexOutput
{
    private readonly PageBlobClient _client;
    private readonly MyBufferedChecksum crc = new(new MyCRC32()); // LUCENENET: marked readonly

    private BufferedStream _stream; // LUCENENET: marked readonly;

    public BlobOutputStream(PageBlobClient client)
    {
        _client = client;
        _stream = BlobStream.OpenBufferedStream(client);
    }

    public override long Position => _stream.Position;
    public override long Checksum => crc.Value;

    public override void Flush()
    {
        _stream.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
    }

    public override void Seek(long pos)
    {
        EnsureStream();
        if (_stream.CanSeek)
        {
            _stream.Seek(pos, SeekOrigin.Begin);
        }
    }

    public override void WriteByte(byte b)
    {
        EnsureStream();
        if (_stream.CanWrite)
        {
            crc.Update(b);
            _stream.WriteByte(b);
        }
    }

    public override void WriteBytes(byte[] b, int offset, int length)
    {
        EnsureStream();
        if (_stream.CanWrite)
        {
            crc.Update(b, offset, length);
            _stream.Write(b, offset, length);
        }
    }

    private void EnsureStream()
    {
        if (!_stream.CanWrite || !_stream.CanSeek)
        {
            _stream = new BufferedStream(new BlobStream(_client), BlobStream.DefaultBufferSize);
        }
    }
}