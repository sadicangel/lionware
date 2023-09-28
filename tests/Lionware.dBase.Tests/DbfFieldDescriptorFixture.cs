using System.Text;

namespace Lionware.dBase;

public sealed class DbfFieldDescriptorFixture : IDisposable
{
    private readonly DbfMemoFile _memoFile;

    public DbfFieldDescriptorFixture()
    {
        _memoFile = new DbfMemoFileV3(new MemoryStream(), writeHeader: true);
        DbfContext = new DbfContextImpl(_memoFile);
    }

    internal IDbfContext DbfContext { get; }

    public void Dispose() => _memoFile.Dispose();
}


file sealed record class DbfContextImpl : IDbfContext
{
    public DbfContextImpl(DbfMemoFile memoFile) => MemoFile = memoFile;

    public Encoding Encoding { get => Encoding.ASCII; }
    public char DecimalSeparator { get => '.'; }
    public DbfMemoFile MemoFile { get; }
}