using System.IO;
using Azure.Storage.Blobs.Specialized;
using Lucene.Net.Store;
using ManagedCode.Storage.Azure;

namespace ManagedCode.Lucene.CloudDirectory;

public class BlobInputStream : IndexInput // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
{
    private readonly PageBlobClient _client;
    private readonly MyBufferedChecksum crc = new(new MyCRC32()); // LUCENENET: marked readonly
    private BufferedStream _stream; // LUCENENET: marked readonly

    public BlobInputStream(PageBlobClient client) : base("BlobInputStream(name=" + client.Name + ")")
    {
        _client = client;
        _stream = BlobStream.OpenBufferedStream(client);
    }

    public override long Position => _stream.Position;
    public override long Length => _stream.Length;

    public override byte ReadByte()
    {
        EnsureStream();
        if (_stream.CanRead)
        {
            var b = _stream.ReadByte();
            return (byte)b;
        }

        return 0;
    }

    public override void ReadBytes(byte[] b, int offset, int len)
    {
        EnsureStream();
        if (_stream.CanRead)
        {
            _ = _stream.Read(b, offset, len);
        }
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

    private void EnsureStream()
    {
        if (!_stream.CanRead || !_stream.CanSeek)
        {
            _stream = new BufferedStream(new BlobStream(_client), BlobStream.DefaultBufferSize);
        }
    }
}