namespace Lionware.dBase;

public abstract class DbfTests : IDisposable
{
    protected string DbfFileName { get; }
    protected string SchemaFileName { get; }
    protected string ValuesFileName { get; }

    protected Dbf ReadOnlyDbf { get; }
    protected DbfRecordDescriptor ReadOnlySchema { get; }
    protected string[][] ReadOnlyValues { get; }

    protected DbfTests(string dbfFileName)
    {
        DbfFileName = dbfFileName;
        SchemaFileName = Path.ChangeExtension(dbfFileName, ".txt");
        ValuesFileName = Path.ChangeExtension(dbfFileName, ".csv");
        ReadOnlyDbf = new Dbf(dbfFileName);
        ReadOnlySchema = new DbfRecordDescriptor(File.ReadLines(SchemaFileName).Select(ReadFieldDescriptor).ToArray());
        ReadOnlyValues = File.ReadLines(ValuesFileName).Skip(1).Select(line => line.Split(',')).ToArray();
    }

    public void Dispose() => ReadOnlyDbf.Dispose();

    public static DbfFieldDescriptor ReadFieldDescriptor(string line)
    {
        var contents = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new DbfFieldDescriptor(
            name: contents[0],
            type: (DbfFieldType)(byte)Char.Parse(contents[1]),
            length: Byte.Parse(contents[2]),
            @decimal: Byte.Parse(contents[3])
        );
    }
}
