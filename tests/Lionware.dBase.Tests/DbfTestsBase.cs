using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Lionware.dBase;

public abstract class DbfTestsBase : IDisposable
{
    protected string DbfFileName { get; }
    protected string SchemaFileName { get; }
    protected string ValuesFileName { get; }

    protected Dbf ReadOnlyDbf { get; }
    protected DbfRecordDescriptor ReadOnlySchema { get; }
    protected IReadOnlyList<string[]> ReadOnlyValues { get; }

    protected DbfTestsBase(string dbfFileName)
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
                DbfFieldType.Ole => str => str ?? String.Empty,
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

    public void Dispose() => ReadOnlyDbf.Dispose();

    public void WithTempDirectory(Action<string> action, [CallerMemberName] string memberName = "")
    {
        var directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), GetUniqueName(memberName)));
        try
        {
            action(directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        string GetUniqueName([CallerMemberName] string memberName = "") => $"{GetType().Name}_{memberName}";
    }
}
