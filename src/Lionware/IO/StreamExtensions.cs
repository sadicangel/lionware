using System.Buffers;

namespace Lionware.IO;

/// <summary>
/// Extensions for <see cref="Stream" />.
/// </summary>s
public static class StreamExtensions
{
    /// <summary>
    /// Inserts data in the stream at <paramref name="offset" />.
    /// </summary>
    /// <param name="stream">The stream to insert data to.</param>
    /// <param name="offset">The position in the stream where the first byte of <paramref name="data"/> is written.</param>
    /// <param name="data">The bytes to insert.</param>
    public static void InsertRange(this Stream stream, long offset, ReadOnlySpan<byte> data)
    {
        Ensure.LessThanOrEqualTo(offset, stream.Length);

        // Don't do anything if data is empty.
        if (data.Length == 0)
            return;

        if (offset < stream.Length)
        {
            const int maxBufferSize = 1024;
            var bytesToCopy = stream.Length - offset;

            var bufferSize = (int)Math.Min(bytesToCopy, maxBufferSize);

            var array = ArrayPool<byte>.Shared.Rent(bufferSize);
            var buffer = array.AsSpan(0, bufferSize);

            int bytesRead;
            for (var i = stream.Length; i > offset; i -= bytesRead)
            {
                // Read block from the end.
                var bytesToRead = (int)Math.Min(bytesToCopy, buffer.Length);
                var position = i - bytesToRead;
                stream.Position = position;
                bytesRead = 0;
                while (bytesToRead > 0)
                {
                    bytesRead = stream.Read(buffer.Slice(bytesRead, bytesToRead));
                    bytesToRead -= bytesRead;
                }
                bytesToCopy -= bytesRead;
                // Write at block start + data offset.
                stream.Position = position + data.Length;
                stream.Write(buffer[..bytesRead]);
            }
            ArrayPool<byte>.Shared.Return(array);
        }

        // Insert the data.
        stream.Position = offset;
        stream.Write(data);
    }

    /// <summary>
    /// Removes data from the stream.
    /// </summary>
    /// <param name="stream">The stream to remove data from.</param>
    /// <param name="range">The range to remove from the stream.</param>
    public static void RemoveRange(this Stream stream, Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(checked((int)stream.Length));
        stream.RemoveRange(offset, length);
    }

    /// <summary>
    /// Removes data from the stream, starting from 
    /// <paramref name="offset" /> up to <paramref name="length" /> bytes.
    /// </summary>
    /// <param name="stream">The stream to remove data from.</param>
    /// <param name="offset">The start position.</param>
    /// <param name="length">The number of bytes to remove.</param>
    public static void RemoveRange(this Stream stream, long offset, long length)
    {
        Ensure.LessThanOrEqualTo(offset + length, stream.Length);

        // Don't do anything if we're already at EOS.
        if (stream.Length == offset)
            return;

        const int maxBufferSize = 1024;
        var bytesToCopy = stream.Length - offset - length;

        var bufferSize = (int)Math.Min(bytesToCopy, maxBufferSize);

        var array = ArrayPool<byte>.Shared.Rent(bufferSize);
        var buffer = array.AsSpan(0, bufferSize);

        int bytesRead;
        for (var i = offset + length; i < stream.Length; i += bytesRead)
        {
            // Read block from current position.
            var bytesToRead = (int)Math.Min(bytesToCopy, buffer.Length);
            stream.Position = i;
            bytesRead = 0;
            while (bytesToRead > 0)
            {
                bytesRead = stream.Read(buffer.Slice(bytesRead, bytesToRead));
                bytesToRead -= bytesRead;
            }
            bytesToCopy -= bytesRead;
            // Write at block i - length offset.
            stream.Position = i - length;
            stream.Write(buffer[..bytesRead]);
        }

        ArrayPool<byte>.Shared.Return(array);
        stream.SetLength(stream.Length - length);
    }
}
