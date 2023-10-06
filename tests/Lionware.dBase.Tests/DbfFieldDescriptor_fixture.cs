using System.Text;

namespace Lionware.dBase;

public sealed class DbfFieldDescriptor_fixture : IDisposable
{
    private readonly DbfMemoFile _memoFile;

    public DbfFieldDescriptor_fixture()
    {
        _memoFile = new DbfMemoFileV3(new MemoryStream(), writeHeader: true);
        DbfContext = new DbfContext(_memoFile);
    }

    internal IDbfContext DbfContext { get; }

    public DbfFieldDescriptor CharacterDescriptor { get; } = DbfFieldDescriptor.Character("character", 50);
    public DbfFieldDescriptor NumericDescriptor { get; } = DbfFieldDescriptor.Numeric("numeric", @decimal: 3);
    public DbfFieldDescriptor FloatDescriptor { get; } = DbfFieldDescriptor.Float("float", @decimal: 3);
    public DbfFieldDescriptor Int32Descriptor { get; } = DbfFieldDescriptor.Int32("int32");
    public DbfFieldDescriptor DoubleDescriptor { get; } = DbfFieldDescriptor.Double("double");
    public DbfFieldDescriptor AutoIncrementDescriptor { get; } = DbfFieldDescriptor.AutoIncrement("autoincrement");
    public DbfFieldDescriptor DateDescriptor { get; } = DbfFieldDescriptor.Date("date");
    public DbfFieldDescriptor TimestampDescriptor { get; } = DbfFieldDescriptor.Timestamp("timestamp");
    public DbfFieldDescriptor LogicalDescriptor { get; } = DbfFieldDescriptor.Logical("logical");
    public DbfFieldDescriptor MemoDescriptor { get; } = DbfFieldDescriptor.Memo("memo", 50);
    public DbfFieldDescriptor BinaryDescriptor { get; } = DbfFieldDescriptor.Binary("binary", 50);
    public DbfFieldDescriptor OleDescriptor { get; } = DbfFieldDescriptor.Ole("ole", 50);
    public DbfFieldDescriptor CurrencyDescriptor { get; } = DbfFieldDescriptor.Currency("currency", 50);
    public DbfFieldDescriptor NullFlagsDescriptor { get; } = DbfFieldDescriptor.NullFlags("null_flags", 1);

    public void Dispose() => _memoFile.Dispose();
}


file sealed record class DbfContext : IDbfContext
{
    public DbfContext(DbfMemoFile memoFile) => MemoFile = memoFile;

    public Encoding Encoding { get => Encoding.ASCII; }
    public char DecimalSeparator { get => '.'; }
    public DbfMemoFile MemoFile { get; }
}