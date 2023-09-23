using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;
public sealed class DbfFieldDescriptorTests
{
    private sealed record class DbfContextImpl : IDbfContext
    {
        public Encoding Encoding { get => Encoding.ASCII; }
        public char DecimalSeparator { get => '.'; }
        public DbfMemoFile? MemoFile { get; }
    }

    private static readonly IDbfContext DbfContext = new DbfContextImpl();

    private static readonly Dictionary<DbfFieldDescriptor, object> Values = new()
    {
        [DbfFieldDescriptor.Boolean("bool_field")] = true,
        [DbfFieldDescriptor.Byte("byte_field")] = (long)128,
        [DbfFieldDescriptor.Int64("i16_field")] = (long)Int16.MaxValue,
        [DbfFieldDescriptor.Int32("i32_field")] = (long)Int32.MaxValue,
        [DbfFieldDescriptor.Int64("i64_field")] = Int64.MaxValue,
        [DbfFieldDescriptor.Single("f32_field")] = (double)Single.MaxValue / 2,
        [DbfFieldDescriptor.Double("f64_field")] = Double.MaxValue / 2,
        [DbfFieldDescriptor.Date("date_field")] = new DateOnly(1987, 3, 29),
        [DbfFieldDescriptor.Timestamp("timestamp_field")] = new DateTime(1987, 3, 29, 0, 0, 0),
        [DbfFieldDescriptor.Text("field_name", 50)] = "some long string with less than 50 chars",
    };

    public static IEnumerable<object[]> GetValuesWithSourceData()
    {
        return Values.Select(v => new object[]
        {
            v.Key,
            v.Value switch {
                bool b => new byte[] { (byte)(b ? 'T' : 'F') },
                DateTime d => GetTimestampData(d),
                DateOnly d => Encoding.ASCII.GetBytes(d.ToString("yyyyMMdd")),
                _ => Encoding.ASCII.GetBytes(v.Value.ToString()!)
            },
            v.Value
        });

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
    public void DbfFieldDescriptor_Read_ReadsFields(DbfFieldDescriptor descriptor, byte[] source, object expectedValue)
    {
        var field = descriptor.CreateReader().Invoke(source, DbfContext);
        Assert.Equal(expectedValue, field.Value);
    }
}
