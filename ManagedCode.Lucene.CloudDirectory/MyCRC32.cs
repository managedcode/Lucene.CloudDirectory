namespace ManagedCode.Lucene.CloudDirectory;

internal class MyCRC32
{
    private static readonly uint[] crcTable = InitializeCRCTable();

    private uint crc;

    public long Value => crc & 0xffffffffL;

    private static uint[] InitializeCRCTable()
    {
        var crcTable = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 8; --k >= 0;)
            {
                if ((c & 1) != 0)
                {
                    c = 0xedb88320 ^ (c >> 1);
                }
                else
                {
                    c = c >> 1;
                }
            }

            crcTable[n] = c;
        }

        return crcTable;
    }

    public void Reset()
    {
        crc = 0;
    }

    public void Update(int bval)
    {
        var c = ~crc;
        c = crcTable[(c ^ bval) & 0xff] ^ (c >> 8);
        crc = ~c;
    }

    public void Update(byte[] buf, int off, int len)
    {
        var c = ~crc;
        while (--len >= 0)
        {
            c = crcTable[(c ^ buf[off++]) & 0xff] ^ (c >> 8);
        }

        crc = ~c;
    }

    public void Update(byte[] buf)
    {
        Update(buf, 0, buf.Length);
    }
}