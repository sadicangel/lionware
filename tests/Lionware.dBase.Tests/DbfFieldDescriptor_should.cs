using AutoFixture;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;

public sealed class DbfFieldDescriptor_should : IClassFixture<DbfFieldDescriptorFixture>
{
    private readonly DbfFieldDescriptorFixture _sharedFixture;

    public DbfFieldDescriptor_should(DbfFieldDescriptorFixture fixture)
    {
        _sharedFixture = fixture;
    }

    [Fact]
    public void Have_a_test_for_each_type()
    {
        var allTypes = Enum.GetValues<DbfType>();

        var testedTypes = _sharedFixture
            .GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(DbfFieldDescriptor))
            .Select(p => ((DbfFieldDescriptor)p.GetValue(_sharedFixture)!).Type)
            .ToArray();

        var untestedTypes = allTypes.Except(testedTypes);

        Assert.Empty(untestedTypes);
    }

    private void AssertRead(in DbfFieldDescriptor descriptor)
    {
        var expectedValue = descriptor.CreateRandomValue();
        var source = descriptor.CreateByteArray(expectedValue);
        var actualValue = descriptor.CreateReader().Invoke(source, _sharedFixture.DbfContext);

        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Read_Character_value() => AssertRead(_sharedFixture.CharacterDescriptor);

    [Fact]
    public void Read_Numeric_value() => AssertRead(_sharedFixture.NumericDescriptor);

    [Fact]
    public void Read_Float_value() => AssertRead(_sharedFixture.FloatDescriptor);

    [Fact]
    public void Read_Int32_value() => AssertRead(_sharedFixture.Int32Descriptor);

    [Fact]
    public void Read_Double_value() => AssertRead(_sharedFixture.DoubleDescriptor);

    [Fact]
    public void Read_AutoIncrement_value() => AssertRead(_sharedFixture.AutoIncrementDescriptor);

    [Fact]
    public void Read_Date_value() => AssertRead(_sharedFixture.DateDescriptor);

    [Fact]
    public void Read_Timestamp_value() => AssertRead(_sharedFixture.TimestampDescriptor);

    [Fact]
    public void Read_Logical_value() => AssertRead(_sharedFixture.LogicalDescriptor);

    [Fact]
    public void Read_Memo_value()
    {
        var expectedValue = String.Concat(Enumerable.Repeat(TestExtensions.CreateRandomValue<string>(), Random.Shared.Next(1, 33)));
        var memoIndex = _sharedFixture.DbfContext.MemoFile!.Append(expectedValue);
        var source = Encoding.ASCII.GetBytes(String.Format("{0,10}", memoIndex));
        var actualValue = _sharedFixture.MemoDescriptor.CreateReader().Invoke(source, _sharedFixture.DbfContext);

        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void Read_Binary_value() => AssertRead(_sharedFixture.BinaryDescriptor);

    [Fact]
    public void Read_Ole_value() => AssertRead(_sharedFixture.OleDescriptor);

    private void AssertWrite(in DbfFieldDescriptor descriptor)
    {
        var value = descriptor.CreateRandomValue();
        var expectedSource = descriptor.CreateByteArray(value);

        var actualSource = new byte[expectedSource.Length];
        descriptor.CreateWriter().Invoke(value, actualSource, _sharedFixture.DbfContext);

        Assert.Equal(expectedSource, actualSource);
    }

    [Fact]
    public void Write_Character_value() => AssertWrite(_sharedFixture.CharacterDescriptor);

    [Fact]
    public void Write_Numeric_value() => AssertWrite(_sharedFixture.NumericDescriptor);

    [Fact]
    public void Write_Float_value() => AssertWrite(_sharedFixture.FloatDescriptor);

    [Fact]
    public void Write_Int32_value() => AssertWrite(_sharedFixture.Int32Descriptor);

    [Fact]
    public void Write_Double_value() => AssertWrite(_sharedFixture.DoubleDescriptor);

    [Fact]
    public void Write_AutoIncrement_value() => AssertWrite(_sharedFixture.AutoIncrementDescriptor);

    [Fact]
    public void Write_Date_value() => AssertWrite(_sharedFixture.DateDescriptor);

    [Fact]
    public void Write_Timestamp_value() => AssertWrite(_sharedFixture.TimestampDescriptor);

    [Fact]
    public void Write_Logical_value() => AssertWrite(_sharedFixture.LogicalDescriptor);

    [Fact]
    public void Write_Memo_value()
    {
        var expectedValue = String.Concat(Enumerable.Repeat(TestExtensions.CreateRandomValue<string>(), Random.Shared.Next(1, 33)));
        var memoIndex = _sharedFixture.DbfContext.MemoFile!.NextAvailableIndex;
        var expectedSource = Encoding.ASCII.GetBytes(String.Format("{0,10}", memoIndex));

        var actualSource = new byte[expectedSource.Length];
        _sharedFixture.MemoDescriptor.CreateWriter().Invoke(expectedValue, actualSource, _sharedFixture.DbfContext);

        Assert.Equal(expectedSource, actualSource);
        Assert.Equal(expectedValue, _sharedFixture.DbfContext.MemoFile![memoIndex]);
    }

    [Fact]
    public void Write_Binary_value() => AssertWrite(_sharedFixture.BinaryDescriptor);

    [Fact]
    public void Write_Ole_value() => AssertWrite(_sharedFixture.OleDescriptor);

}

file static class TestExtensions
{
    private static readonly Fixture AutoFixture = new();

    public static T CreateRandomValue<T>() => AutoFixture.Create<T>();

    public static object? CreateRandomValue(this DbfFieldDescriptor descriptor) => descriptor.Type switch
    {
        DbfType.Character => AutoFixture.Create<string>(),
        DbfType.Numeric => AutoFixture.Create<double>(),
        DbfType.Float => AutoFixture.Create<double>(),
        DbfType.Int32 => AutoFixture.Create<int>(),
        DbfType.Double => AutoFixture.Create<double>(),
        DbfType.AutoIncrement => AutoFixture.Create<int>(),
        DbfType.Date => DateOnly.FromDateTime(AutoFixture.Create<DateTime>()),
        DbfType.Timestamp => DateTime.Parse(AutoFixture.Create<DateTime>().ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
        DbfType.Logical => AutoFixture.Create<bool>(),
        DbfType.Memo => null,
        DbfType.Binary => null,
        DbfType.Ole => null,
        _ => throw new InvalidOperationException($"Unexpected type {descriptor.Type}")
    };

    public static byte[] CreateByteArray(this DbfFieldDescriptor descriptor, object? value) => descriptor.Type switch
    {
        DbfType.Character => Encoding.ASCII.GetBytes((string)value!),
        DbfType.Numeric => Encoding.ASCII.GetBytes(((double)value!).ToString($"F{descriptor.Decimal}")),
        DbfType.Float => Encoding.ASCII.GetBytes(((double)value!).ToString($"F{descriptor.Decimal}")),
        DbfType.Int32 => BitConverter.GetBytes((int)value!),
        DbfType.Double => BitConverter.GetBytes((double)value!),
        DbfType.AutoIncrement => BitConverter.GetBytes((int)value!),
        DbfType.Date => Encoding.ASCII.GetBytes(((DateOnly)value!).ToString("yyyyMMdd")),
        DbfType.Timestamp => GetTimestampData((DateTime)value!),
        DbfType.Logical => new byte[] { (byte)((bool)value! ? 'T' : 'F') },
        DbfType.Memo => value is string str ? Encoding.ASCII.GetBytes(str) : GetEmptyArray(descriptor.Length),
        DbfType.Binary => value is string str ? Encoding.ASCII.GetBytes(str) : GetEmptyArray(descriptor.Length),
        DbfType.Ole => value is string str ? Encoding.ASCII.GetBytes(str) : GetEmptyArray(descriptor.Length),
        _ => throw new InvalidOperationException($"Unexpected type {descriptor.Type}")
    };


    private static byte[] GetEmptyArray(int length)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, (byte)' ');
        return bytes;
    }

    private static byte[] GetTimestampData(DateTime timestamp)
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