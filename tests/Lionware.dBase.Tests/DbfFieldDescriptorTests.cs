using System.Text;

namespace Lionware.dBase;
public sealed class DbfFieldDescriptorTests
{
    private static readonly Dictionary<DbfFieldDescriptor, object> Values = new()
    {
        [DbfFieldDescriptor.Boolean("bool_field")] = true,
        [DbfFieldDescriptor.Byte("byte_field")] = (long)128,
        [DbfFieldDescriptor.Int64("i16_field")] = (long)Int16.MaxValue,
        [DbfFieldDescriptor.Int32("i32_field")] = (long)Int32.MaxValue,
        [DbfFieldDescriptor.Int64("i64_field")] = Int64.MaxValue,
        [DbfFieldDescriptor.Single("f32_field")] = (double)Single.MaxValue / 2,
        [DbfFieldDescriptor.Double("f64_field")] = Double.MaxValue / 2,
        [DbfFieldDescriptor.Date("date_field")] = new DateTime(1987, 3, 29),
        [DbfFieldDescriptor.Text("field_name", 50)] = "some long string with less than 50 chars",
    };

    public static IEnumerable<object[]> GetValuesWithSourceData() => Values.Select(v => new object[]
    {
        v.Key,
        v.Value switch {
            bool b => new byte[] { (byte)(b ? 'T' : 'F') },
            DateTime d => Encoding.ASCII.GetBytes(d.ToString("yyyyMMdd")),
            _ => Encoding.ASCII.GetBytes(v.Value.ToString()!)
        },
        v.Value
    });

    [Theory]
    [MemberData(nameof(GetValuesWithSourceData))]
    public void DbfFieldDescriptor_Read_ReadsFields(DbfFieldDescriptor descriptor, byte[] source, object expectedValue)
    {
        var field = descriptor.Read(source, Encoding.ASCII, '.');
        Assert.Equal(expectedValue, field.Value);
    }
}
