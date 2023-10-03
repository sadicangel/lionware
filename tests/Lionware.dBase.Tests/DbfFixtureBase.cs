using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Lionware.dBase;

public abstract class DbfFixtureBase : IDisposable
{
    private bool _disposedValue;
    private Dbf? _readOnlyDbf;
    private DbfSchema? _readOnlySchema;
    private List<string[]>? _readOnlyValues;

    public string DbfFileName { get; }
    public string SchemaFileName { get; }
    public string ValuesFileName { get; }

    public Dbf ReadOnlyDbf { get => _readOnlyDbf ??= new Dbf(DbfFileName); }
    public DbfSchema ReadOnlySchema
    {
        get
        {
            return _readOnlySchema ??= new DbfSchema(File.ReadLines(SchemaFileName).Select(ReadFieldDescriptor).ToArray());

            static DbfFieldDescriptor ReadFieldDescriptor(string line)
            {
                var contents = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return new DbfFieldDescriptor(
                    name: contents[0],
                    type: (DbfType)(byte)Char.Parse(contents[1]),
                    length: Byte.Parse(contents[2]),
                    @decimal: Byte.Parse(contents[3])
                );
            }
        }
    }
    public IReadOnlyList<string[]> ReadOnlyValues
    {
        get
        {
            if (_readOnlyValues is null)
            {
                using var stream = new StreamReader(ValuesFileName);
                using var reader = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture));

                var fieldCount = ReadOnlySchema.FieldCount;
                if (reader.HeaderRecord is null)
                    reader.Read();

                _readOnlyValues = new List<string[]>();

                var formatters = new Func<string, string>[fieldCount];
                for (int i = 0; i < formatters.Length; ++i)
                {
                    ref readonly var descriptor = ref ReadOnlySchema[i];
                    var @decimal = descriptor.Decimal;
                    formatters[i] = descriptor.Type switch
                    {
                        DbfType.Character => str => str ?? String.Empty,
                        DbfType.Numeric or
                        DbfType.Float or
                        DbfType.Int32 or
                        DbfType.Double or
                        DbfType.AutoIncrement => str => String.IsNullOrEmpty(str) ? String.Empty : Convert.ToDouble(str).ToString($"F{@decimal}"),
                        DbfType.Date or
                        DbfType.Timestamp => str => str ?? String.Empty,
                        DbfType.Logical => str => str ?? String.Empty,
                        DbfType.Memo or
                        DbfType.Binary or
                        DbfType.Ole => str => String.IsNullOrEmpty(str) ? String.Empty : OperatingSystem.IsWindows() ? str : str.Replace(Environment.NewLine, "\r\n"),
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
                    _readOnlyValues.Add(fields);
                }
            }
            return _readOnlyValues;
        }
    }

    protected DbfFixtureBase(string dbfFileName)
    {
        DbfFileName = dbfFileName;
        SchemaFileName = Path.ChangeExtension(dbfFileName, ".txt");
        ValuesFileName = Path.ChangeExtension(dbfFileName, ".csv");
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
