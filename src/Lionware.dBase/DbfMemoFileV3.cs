using System.Buffers;
using System.Text;

namespace Lionware.dBase;

internal sealed class DbfMemoFileV3 : DbfMemoFile
{
    internal DbfMemoFileV3(Stream stream, bool writeHeader) : base(stream, DefaultBlockSize, writeHeader) { }

    public override string this[int index]
    {
        get
        {
            Stream.Position = BlockSize * index;
            var writer = new ArrayBufferWriter<byte>(BlockSize);
            var i = -1;
            while (i < 0)
            {
                var buffer = writer.GetSpan(BlockSize)[..BlockSize];
                var bytesRead = Stream.Read(buffer);
                if (bytesRead > 0)
                {
                    i = buffer[..bytesRead].IndexOf(FieldEndMarker);
                    if (i >= 0)
                        bytesRead = i;
                }
                else
                {
                    // We reached EOF. Throw?
                    i = 0;
                }
                writer.Advance(bytesRead);
            }
            return Encoding.UTF8.GetString(writer.WrittenSpan);
        }
    }

    public override int Append(string memo)
    {
        var writer = new ArrayBufferWriter<byte>(Encoding.UTF8.GetMaxByteCount(memo.Length));
        var bytesWritten = Encoding.UTF8.GetBytes(memo, writer);
        writer.Write(FieldEndMarker);
        bytesWritten += 2;

        var paddingBytes = (int)(BlockSize - (bytesWritten % BlockSize));
        writer.GetSpan(paddingBytes)[..paddingBytes].Clear();
        writer.Advance(paddingBytes);

        var index = NextAvailableIndex;

        Stream.Position = index * BlockSize;
        Stream.Write(writer.WrittenSpan);

        UpdateNextAvailableIndex((int)(Stream.Position / BlockSize));

        return index;
    }

    protected override int WriteHeader()
    {
        if (Stream.Length != 0)
            throw new InvalidOperationException("Can only write header on a new memo file.");

        Stream.SetLength(DefaultBlockSize);

        var index = (int)(Stream.Length / BlockSize);

        if (index * BlockSize != Stream.Length)
            Stream.SetLength(index * BlockSize);

        UpdateBlockSize(BlockSize);
        UpdateNextAvailableIndex(index);
        Stream.Position = Stream.Length;
        return index;
    }
}
