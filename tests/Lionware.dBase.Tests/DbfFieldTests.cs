using System.Reflection;

namespace Lionware.dBase;

public sealed class DbfFieldTests
{
    private static readonly object[][] ValidValues = new object[][]
    {
        new object[] { (bool b) => DbfField.Logical(b), true },
        new object[] { (int i) => DbfField.Int32(i), 684692656},
        new object[] { (int i) => DbfField.AutoIncrement(i), 684692656},
        new object[] { (double d) => DbfField.Float(d), 0.058148685515572951},
        new object[] { (double d) => DbfField.Numeric(d), 0.058148685515572951},
        new object[] { (double d) => DbfField.Double(d), 0.058148685515572951},
        new object[] { (DateTime d) => DbfField.Timestamp(d), new DateTime(1987, 3, 29, 6, 30, 57)},
        new object[] { (DateOnly d) => DbfField.Date(d), new DateOnly(1987, 3, 29)},
        new object[] { (string s) => DbfField.Character(s), "some-random-string!" },
        new object[] { (string s) => DbfField.Memo(s), "some-random-string!" },
        new object[] { (string s) => DbfField.Binary(s), "some-random-string!" },
    };

    private static readonly MethodInfo GetValueMethod = typeof(DbfField).GetMethod(nameof(DbfField.GetValue))!;

    public static IEnumerable<object?[]> GetValidValuesData() => ValidValues;

    [Theory]
    [MemberData(nameof(GetValidValuesData))]
    public void DbfField_Constructor_ConstructsValue(Delegate @delegate, object value)
    {
        Assert.Null(Record.Exception(() => @delegate.DynamicInvoke(new object[] { value })));
    }

    public static IEnumerable<object?[]> GetValidValuesWithTypeData() => ValidValues.Select(v => v.Append(v[1].GetType()).ToArray());

    [Theory]
    [MemberData(nameof(GetValidValuesData))]
    public void DbfField_GetValue_GetsCorrectObjectValue(Delegate @delegate, object value)
    {
        var field = (DbfField)@delegate.DynamicInvoke(new object[] { value })!;
        Assert.Equal(value, field.Value);
    }

    [Theory]
    [MemberData(nameof(GetValidValuesWithTypeData))]
    public void DbfField_GetValue_GetsCorrectGenericValue(Delegate @delegate, object value, Type type)
    {
        var field = (DbfField)@delegate.DynamicInvoke(new object[] { value })!;
        var generic = GetValueMethod.MakeGenericMethod(type);
        var result = generic.Invoke(field, null);
        Assert.Equal(value, result);
    }

    public static IEnumerable<object?[]> GetValidNonNullValuesWithTypeData() => ValidValues.Where(v => v is not null).Select(v => new object?[] { v, v?.GetType() ?? typeof(string) });

    [Theory]
    [MemberData(nameof(GetValidValuesWithTypeData))]
    public void DbfField_GetValue_GetsCorrectSpecificValue(Delegate @delegate, object value, Type type)
    {
        var field = (DbfField)@delegate.DynamicInvoke(new object[] { value })!;
        var method = typeof(DbfField).GetMethod($"Get{type.Name}");
        Assert.NotNull(method);
        var result = method.Invoke(field, null);
        Assert.Equal(value, result);
    }

    [Theory]
    [MemberData(nameof(GetValidValuesWithTypeData))]
    public void DbfField_GetValueOrDefault_GetsCorrectDefaultValue(Delegate @delegate, object defaultValue, Type type)
    {
        _ = @delegate;
        var field = new DbfField();
        var method = typeof(DbfField).GetMethod($"Get{type.Name}OrDefault");
        Assert.NotNull(method);
        var result = method.Invoke(field, new object?[] { defaultValue });
        Assert.Equal(defaultValue, result);
    }
}