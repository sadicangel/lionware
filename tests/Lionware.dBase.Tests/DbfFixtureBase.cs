using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Lionware.dBase;

public abstract class DbfFixtureBase : IDisposable
{
    private bool _disposedValue;

    public string DbfFileName { get; }
    public string SchemaFileName { get; }
    public string ValuesFileName { get; }

    public Dbf ReadOnlyDbf { get; }
    public DbfRecordDescriptor ReadOnlySchema { get; }
    public IReadOnlyList<string[]> ReadOnlyValues { get; }

    protected DbfFixtureBase(string dbfFileName)
    {
        DbfFileName = dbfFileName;
        SchemaFileName = Path.ChangeExtension(dbfFileName, ".txt");
        ValuesFileName = Path.ChangeExtension(dbfFileName, ".csv");
        ReadOnlyDbf = new Dbf(dbfFileName);
        ReadOnlySchema = new DbfRecordDescriptor(File.ReadLines(SchemaFileName).Select(ReadFieldDescriptor).ToArray());
        using var stream = new StreamReader(ValuesFileName);
        using var reader = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture));

        var fieldCount = ReadOnlySchema.Count;
        if (reader.HeaderRecord is null)
            reader.Read();

        var readOnlyValues = new List<string[]>();

        var formatters = new Func<string, string>[ReadOnlySchema.Count];
        for (int i = 0; i < formatters.Length; ++i)
        {
            ref readonly var descriptor = ref ReadOnlySchema[i];
            var @decimal = descriptor.Decimal;
            formatters[i] = descriptor.Type switch
            {
                DbfFieldType.Character => str => str ?? String.Empty,
                DbfFieldType.Numeric or
                DbfFieldType.Float or
                DbfFieldType.Int32 or
                DbfFieldType.Double or
                DbfFieldType.AutoIncrement => str => String.IsNullOrEmpty(str) ? String.Empty : Convert.ToDouble(str).ToString($"F{@decimal}"),
                DbfFieldType.Date or
                DbfFieldType.Timestamp => str => str ?? String.Empty,
                DbfFieldType.Logical => str => str ?? String.Empty,
                DbfFieldType.Memo or
                DbfFieldType.Binary or
                DbfFieldType.Ole => str => String.IsNullOrEmpty(str) ? String.Empty : OperatingSystem.IsWindows() ? str : str.Replace(Environment.NewLine, "\r\n"),
                _ => throw new NotImplementedException(),
            };
        }

        while (reader.Read())
        {
            var fields = new string[fieldCount];
            for (int i = 0; i < fields.Length; ++i)
            {
                var value = reader.GetField(i);
                if (value is not null)
                    value = formatters[i].Invoke(value);
                fields[i] = value ?? String.Empty;
            }
            readOnlyValues.Add(fields);
        }
        ReadOnlyValues = readOnlyValues;

        static DbfFieldDescriptor ReadFieldDescriptor(string line)
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                ReadOnlyDbf.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
