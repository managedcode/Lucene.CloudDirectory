using System;
using System.Runtime.CompilerServices;

namespace ManagedCode.Lucene.CloudDirectory;

internal class MyBufferedChecksum
{
    /// <summary>
    ///     Default buffer size: 256
    /// </summary>
    public const int DEFAULT_BUFFERSIZE = 256;

    private readonly byte[] buffer;
    private readonly MyCRC32 @in;
    private int upto;

    /// <summary>
    ///     Create a new <see cref="Lucene.Net.Store.BufferedChecksum" /> with <see cref="DEFAULT_BUFFERSIZE" />
    /// </summary>
    public MyBufferedChecksum(MyCRC32 @in)
        : this(@in, DEFAULT_BUFFERSIZE)
    {
    }

    /// <summary>
    ///     Create a new <see cref="Lucene.Net.Store.BufferedChecksum" /> with the specified <paramref name="bufferSize" />
    /// </summary>
    public MyBufferedChecksum(MyCRC32 @in, int bufferSize)
    {
        this.@in = @in;
        buffer = new byte[bufferSize];
    }

    public virtual long Value
    {
        get
        {
            Flush();
            return @in.Value;
        }
    }

    public virtual void Update(int b)
    {
        if (upto == buffer.Length)
        {
            Flush();
        }

        buffer[upto++] = (byte)b;
    }

    // LUCENENET specific overload for updating a whole byte[] array
    public virtual void Update(byte[] b)
    {
        Update(b, 0, b.Length);
    }

    public virtual void Update(byte[] b, int off, int len)
    {
        if (len >= buffer.Length)
        {
            Flush();
            @in.Update(b, off, len);
        }
        else
        {
            if (upto + len > buffer.Length)
            {
                Flush();
            }

            Buffer.BlockCopy(b, off, buffer, upto, len);
            upto += len;
        }
    }

    public virtual void Reset()
    {
        upto = 0;
        @in.Reset();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Flush()
    {
        if (upto > 0)
        {
            @in.Update(buffer, 0, upto);
        }

        upto = 0;
    }
}