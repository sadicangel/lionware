using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;
public sealed class DbfFieldDescriptorTests
{
    private static readonly IDbfContext DbfContext = new DbfContextImpl();

    private static readonly Dictionary<DbfFieldDescriptor, object?> Values = new()
    {
        [DbfFieldDescriptor.Logical("logical")] = true,
        [DbfFieldDescriptor.Numeric("numeric")] = Double.MaxValue / 2,
        [DbfFieldDescriptor.Float("float")] = Double.MaxValue / 2,
        [DbfFieldDescriptor.Int32("int32")] = Int32.MaxValue / 2,
        [DbfFieldDescriptor.AutoIncrement("autoincrement")] = Int32.MaxValue / 2,
        [DbfFieldDescriptor.Double("double")] = Double.MaxValue / 2,
        [DbfFieldDescriptor.Date("date")] = new DateOnly(1987, 3, 29),
        [DbfFieldDescriptor.Timestamp("timestamp")] = new DateTime(1987, 3, 29, 0, 0, 0),
        [DbfFieldDescriptor.Character("character", 50)] = "some long string with less than 50 chars",
        [DbfFieldDescriptor.Memo("memo", 50)] = null,
        [DbfFieldDescriptor.Binary("binary", 50)] = null,
        [DbfFieldDescriptor.Ole("ole", 50)] = null,
    };

    public static IEnumerable<object?[]> GetValuesWithSourceData()
    {
        return Values.Select(v => new object?[]
        {
            v.Key,
            (v.Key.Type, v.Value) switch {
                (_, null) => GetEmptyArray(v.Key.Length),
                (DbfFieldType.Logical, bool b) => new byte[] { (byte)(b ? 'T' : 'F') },
                (DbfFieldType.Int32, int i) => BitConverter.GetBytes(i),
                (DbfFieldType.AutoIncrement, int i) => BitConverter.GetBytes(i),
                (DbfFieldType.Double, double d) => BitConverter.GetBytes(d),
                (DbfFieldType.Date, DateOnly d) => Encoding.ASCII.GetBytes(d.ToString("yyyyMMdd")),
                (DbfFieldType.Timestamp, DateTime d) => GetTimestampData(d),
                _ => Encoding.ASCII.GetBytes(v.Value.ToString()!)
            },
            v.Value
        });

        static byte[] GetEmptyArray(int length)
        {
            var bytes = new byte[length];
            Array.Fill(bytes, (byte)' ');
            return bytes;
        }

        static byte[] GetTimestampData(DateTime timestamp)
        {
            const int JulianOffsetToDateTime = 1721426;

            var buffer = new byte[8];
            var target = buffer.AsSpan();
            var timespan = timestamp - new DateTime(1, 1, 1);
            var timestampDate = JulianOffsetToDateTime + (int)timespan.TotalDays;
            MemoryMarshal.Write(target[..4], ref timestampDate);
            var timestampTime = (int)(timespan - TimeSpan.FromDays(timespan.Days)).TotalMilliseconds;
            MemoryMarshal.Write(target[4..], ref timestampTime);
            return buffer;
        }
    }

    [Theory]
    [MemberData(nameof(GetValuesWithSourceData))]
    public void DbfFieldDescriptor_Read_ReadsFields(DbfFieldDescriptor descriptor, byte[] source, object? expectedValue)
    {
        Debug.WriteLine($"{expectedValue?.GetType().Name ?? nameof(Object)}: {expectedValue}");
        var field = descriptor.CreateReader().Invoke(source, DbfContext);
        Assert.Equal(expectedValue, field.Value);
    }
}

file sealed record class DbfContextImpl : IDbfContext
{
    public Encoding Encoding { get => Encoding.ASCII; }
    public char DecimalSeparator { get => '.'; }
    public DbfMemoFile? MemoFile { get; }
}