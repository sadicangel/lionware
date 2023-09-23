using System.Buffers;
using System.Text;

namespace Lionware.dBase;

internal sealed class DbfMemoFile03 : DbfMemoFile
{
    internal DbfMemoFile03(Stream stream) : base(stream) { }

    public override string this[int index]
    {
        get
        {
            Stream.Position = BlockSize * index;
            var writer = new ArrayBufferWriter<byte>(BlockSize);
            var done = false;
            while (!done)
            {
                var buffer = writer.GetSpan(BlockSize)[..BlockSize];
                var bytesRead = Stream.Read(buffer);
                var i = buffer[..bytesRead].IndexOf(FieldEndMarker);
                if (i >= 0)
                {
                    buffer = buffer[..i];
                    bytesRead = i;
                    done = true;
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

        var index = (int)(Stream.Length / BlockSize);
        Stream.Position = Stream.Length;
        Stream.Write(writer.WrittenSpan);
        return index;
    }
}
