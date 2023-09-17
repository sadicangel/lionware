using System.Reflection;

namespace Lionware.dBase;

public sealed class DbfFieldTests
{
    private static readonly dynamic?[] ValidValues = new dynamic?[]
    {
        null,
        true,
        (byte)123,
        (short)12345,
        684692656,
        9053598978304173029,
        0.05814f,
        0.058148685515572951,
        new DateTime(1987, 3, 29, 6, 30, 57),
        new DateOnly(1987, 3, 29),
        "some-random-string!",
    };

    private static readonly MethodInfo GetValueMethod = typeof(DbfField).GetMethod(nameof(DbfField.GetValue))!;

    public static IEnumerable<object?[]> GetValidValuesData() => ValidValues.Select(v => new object?[] { v });

    [Theory]
    [MemberData(nameof(GetValidValuesData))]
    public void DbfField_Constructor_ConstructsValue(dynamic? value)
    {
        Assert.Null(Record.Exception(() => new DbfField(value)));
    }

    public static IEnumerable<object?[]> GetValidValuesWithTypeData() => ValidValues.Select(v => new object?[] { v, v?.GetType() ?? typeof(string) });

    [Theory]
    [MemberData(nameof(GetValidValuesData))]
    public void DbfField_GetValue_GetsCorrectObjectValue(dynamic? value)
    {
        var field = new DbfField(value);
        Assert.Equal(value, field.Value);
    }

    [Theory]
    [MemberData(nameof(GetValidValuesWithTypeData))]
    public void DbfField_GetValue_GetsCorrectGenericValue(dynamic? value, Type type)
    {
        var field = new DbfField(value);
        var generic = GetValueMethod.MakeGenericMethod(type);
        var result = generic.Invoke(field, null);
        Assert.Equal(value, result);
    }

    public static IEnumerable<object?[]> GetValidNonNullValuesWithTypeData() => ValidValues.Where(v => v is not null).Select(v => new object?[] { v, v?.GetType() ?? typeof(string) });

    [Theory]
    [MemberData(nameof(GetValidNonNullValuesWithTypeData))]
    public void DbfField_GetValue_GetsCorrectSpecificValue(dynamic? value, Type type)
    {
        var field = new DbfField(value);
        var method = typeof(DbfField).GetMethod($"Get{type.Name}");
        Assert.NotNull(method);
        var result = method.Invoke(field, null);
        Assert.Equal(value, result);
    }

    [Theory]
    [MemberData(nameof(GetValidNonNullValuesWithTypeData))]
    public void DbfField_GetValueOrDefault_GetsCorrectDefaultValue(dynamic? defaultValue, Type type)
    {
        var field = new DbfField();
        var method = typeof(DbfField).GetMethod($"Get{type.Name}OrDefault");
        Assert.NotNull(method);
        var result = method.Invoke(field, new object?[] { defaultValue });
        Assert.Equal(defaultValue, result);
    }
}