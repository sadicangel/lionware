using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lionware.dBase;

/// <summary>
/// Represents a field of a <see cref="DbfRecord" />.
/// </summary>
/// <seealso cref="IEquatable{T}" />
/// <remarks>
/// The field is defined by a <see cref="DbfFieldDescriptor" />.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
[StructLayout(LayoutKind.Explicit, Size = 24)]
public readonly struct DbfField : IEquatable<DbfField>
{
    internal enum ClrType : byte
    {
        Empty = 0,
        Boolean = 3,
        Int32 = 9,
        Double = 14,
        DateTime = 16,
        DateOnly = 17,
        String = 18
    }

    // type code of the value.
    [FieldOffset(0)] internal readonly ClrType _clrType;
    // dbf type of the value.
    [FieldOffset(1)] internal readonly DbfFieldType _dbfType;
    // true if value is stored locally; false if it's a value on the heap.
    [FieldOffset(2)] private readonly bool _isInline;
    // size in bytes of the value (0 when not stored locally).
    [FieldOffset(3)] private readonly byte _inlineSize;
    // total number of characters.
    [FieldOffset(4)] private readonly byte _length;
    // number of decimal characters.
    [FieldOffset(5)] private readonly byte _decimal;
    // reserved
    [FieldOffset(6)] private readonly ushort _reserved;
    // first byte of inline val (8 bytes)
    [FieldOffset(8)] private readonly byte _inlineValue;
    // reference to object in heap (4 or 8 bytes)
    [FieldOffset(16)] private readonly string? _referenceValue;

    private DbfField(string? value, DbfFieldType dbfType, byte length, byte @decimal)
    {
        _clrType = value is not null ? ClrType.String : ClrType.Empty;
        _dbfType = dbfType;
        _isInline = false;
        _inlineSize = 0;
        _length = length;
        _decimal = @decimal;
        _reserved = 0;
        _inlineValue = 0;
        _referenceValue = value;
    }

    private DbfField(ClrType clrType, DbfFieldType dbfType, byte inlineSize, byte length, byte @decimal)
    {
        _clrType = clrType;
        _dbfType = dbfType;
        _isInline = true;
        _inlineSize = inlineSize;
        _length = length;
        _decimal = @decimal;
        _reserved = 0;
        _inlineValue = 0;
        _referenceValue = null;
    }

    private static DbfField CreateInline<T>(T value, ClrType clrType, DbfFieldType dbfType, byte length, byte @decimal) where T : struct
    {
        var field = new DbfField(clrType, dbfType, (byte)Unsafe.SizeOf<T>(), length, @decimal);
        MemoryMarshal.Write(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(field._inlineValue), field._inlineSize), ref value);
        return field;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    public readonly bool IsNull { get => _clrType is ClrType.Empty; }

    /// <summary>
    /// Gets the span stored in this instance, sliced to the correct size.
    /// If not an inline value, the span is empty.
    /// </summary>
    internal readonly ReadOnlySpan<byte> InlineValue { get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _inlineValue), 8); }

    /// <summary>
    /// Gets the reference stored by this instance. The value is <see langword="null"/>
    /// ff type is <see cref="ClrType.Empty"/> or not a reference value.
    /// </summary>
    internal readonly string? ReferenceValue { get => _referenceValue; }

    /// <summary>
    /// Gets the value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <remarks>The value is boxed for value types.</remarks>
    public object? Value
    {
        get => _clrType switch
        {
            ClrType.Boolean => ReadInlineValue<bool>(),
            ClrType.Int32 => ReadInlineValue<int>(),
            ClrType.Double => ReadInlineValue<double>(),
            ClrType.DateTime => ReadInlineValue<DateTime>(),
            ClrType.DateOnly => ReadInlineValue<DateOnly>(),
            _ => _referenceValue,
        };
    }

    /// <summary>
    /// Gets the value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <remarks>The value is boxed for value types.</remarks>
    public T? GetValue<T>() => (T?)Value;

    private T ThrowInvalidType<T>(ClrType expectedType) => throw new InvalidOperationException($"Cannot convert value to {expectedType}. Field type is {_clrType}");
    private T ThrowInvalidType<T>(params ClrType[] expectedTypes)
    {
        switch (expectedTypes.Length)
        {
            case 0:
                throw new InvalidOperationException($"Unexpected field type {_clrType}");
            case > 1:
                throw new InvalidOperationException($"Cannot convert value to {String.Join(", ", expectedTypes.Take(expectedTypes.Length - 1).Select(e => e.ToString()))} or {expectedTypes[^1]}. Field type is {_clrType}");
            default:
                return ThrowInvalidType<T>(expectedTypes[0]);
        }
    }

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Character" />.
    /// </summary>
    /// <param name="type">The type of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores an empty value.
    /// </returns>
    public static DbfField Null(DbfFieldType type, byte length = 10, byte @decimal = 0) => new(value: null, type, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Character" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Character(string? value, byte length = 10) => new(value, DbfFieldType.Character, length, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Memo" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Memo(string? value, byte length = 10) => new(value, DbfFieldType.Memo, length, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Binary" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Binary(string? value, byte length = 10) => new(value, DbfFieldType.Binary, length, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Ole" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Ole(string? value, byte length = 10) => new(value, DbfFieldType.Ole, length, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Date" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Date(DateOnly value) => CreateInline(value, ClrType.DateOnly, DbfFieldType.Date, length: 0, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Float" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    /// <remarks>Identical to <see cref="DbfFieldType.Numeric" />; maintained for compatibility.</remarks>
    public static DbfField Float(double value, byte length = 10, byte @decimal = 0) => CreateInline(value, ClrType.Double, DbfFieldType.Float, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Numeric(double value, byte length = 20, byte @decimal = 10) => CreateInline(value, ClrType.Double, DbfFieldType.Numeric, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Logical" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Logical(bool value) => CreateInline(value, ClrType.Boolean, DbfFieldType.Logical, length: 1, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Timestamp" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Timestamp(DateTime value) => CreateInline(value, ClrType.DateTime, DbfFieldType.Timestamp, length: 8, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Int32" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Int32(int value) => CreateInline(value, ClrType.Int32, DbfFieldType.Int32, length: 4, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.AutoIncrement" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField AutoIncrement(int value) => CreateInline(value, ClrType.Int32, DbfFieldType.AutoIncrement, length: 4, @decimal: 0);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Double" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Double(double value) => CreateInline(value, ClrType.Double, DbfFieldType.Double, length: 4, @decimal: 0);

    /// <summary>
    /// Gets the <see cref="bool"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public bool GetBoolean() => _clrType is not ClrType.Boolean ? ThrowInvalidType<bool>(ClrType.Boolean) : ReadInlineValue<bool>();

    /// <summary>
    /// Gets the <see cref="bool"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public bool GetBooleanOrDefault(bool defaultValue = default) => _clrType is ClrType.Empty ? defaultValue : GetBoolean();

    /// <summary>
    /// Gets the <see cref="int"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public int GetInt32() => _clrType is not ClrType.Int32 ? ThrowInvalidType<int>(ClrType.Int32) : ReadInlineValue<int>();

    /// <summary>
    /// Gets the <see cref="int"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public int GetInt32OrDefault(int defaultValue = default) => _clrType is ClrType.Empty ? defaultValue : GetInt32();

    /// <summary>
    /// Gets the <see cref="double"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public double GetDouble() => _clrType is not ClrType.Double ? ThrowInvalidType<double>(ClrType.Double) : ReadInlineValue<double>();

    /// <summary>
    /// Gets the <see cref="double"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public double GetDoubleOrDefault(double defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetDouble();
    }

    /// <summary>
    /// Gets the <see cref="DateTime"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public DateTime GetDateTime() => _clrType switch
    {
        ClrType.DateTime => ReadInlineValue<DateTime>(),
        ClrType.DateOnly => ReadInlineValue<DateOnly>().ToDateTime(TimeOnly.MinValue),
        _ => ThrowInvalidType<DateTime>(ClrType.DateTime, ClrType.DateOnly),
    };

    /// <summary>
    /// Gets the <see cref="DateTime"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public DateTime GetDateTimeOrDefault(DateTime defaultValue = default) => _clrType is ClrType.Empty ? defaultValue : GetDateTime();

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public DateOnly GetDateOnly() => _clrType switch
    {
        ClrType.DateTime => DateOnly.FromDateTime(ReadInlineValue<DateTime>()),
        ClrType.DateOnly => ReadInlineValue<DateOnly>(),
        _ => ThrowInvalidType<DateOnly>(ClrType.DateTime, ClrType.DateOnly),
    };

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public DateOnly GetDateOnlyOrDefault(DateOnly defaultValue = default) => _clrType is ClrType.Empty ? defaultValue : GetDateOnly();

    /// <summary>
    /// Gets the <see cref="string"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public string GetString() => _clrType is not ClrType.String ? ThrowInvalidType<string>(ClrType.String) : _referenceValue!;

    /// <summary>
    /// Gets the <see cref="string"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? GetStringOrDefault(string? defaultValue = default) => _clrType is ClrType.Empty ? defaultValue : GetString();

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="String" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        if (_clrType is ClrType.Empty)
            return String.Empty;

        return _dbfType switch
        {
            DbfFieldType.Character or
            DbfFieldType.Memo or
            DbfFieldType.Binary or
            DbfFieldType.Ole => _referenceValue ?? String.Empty,

            DbfFieldType.Numeric or
            DbfFieldType.Float or
            DbfFieldType.Int32 or
            DbfFieldType.Double or
            DbfFieldType.AutoIncrement => Convert.ToDouble(Value).ToString($"F{_decimal}"),
            DbfFieldType.Date => GetDateOnly().ToString("yyyyMMdd"),
            DbfFieldType.Timestamp => GetDateTime().ToString("o"),
            DbfFieldType.Logical => GetBooleanOrDefault() ? "T" : "F",
            _ => throw new NotImplementedException(),
        };
    }

    private string GetDebuggerDisplay() => ToString();

    internal T ReadInlineValue<T>() where T : struct => MemoryMarshal.Read<T>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(_inlineValue), _inlineSize));

    internal void WriteInlineValue<T>(T value) where T : struct => MemoryMarshal.Write(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(_inlineValue), _inlineSize), ref value);

    /// <inheritdoc />
    public bool Equals(DbfField other)
    {
        if (_clrType != other._clrType)
            return false;

        if (_inlineSize != other._inlineSize)
            return false;

        Debug.Assert(_isInline == other._isInline);
        if (!_isInline)
        {
            return _referenceValue == other._referenceValue;
        }
        else
        {
            for (int i = 0; i < _inlineSize; ++i)
            {
                ref var l = ref Unsafe.Add(ref Unsafe.AsRef(_inlineValue), i);
                ref var r = ref Unsafe.Add(ref Unsafe.AsRef(other._inlineValue), i);
                if (l != r)
                    return false;
            }
            return true;
        }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DbfField field && Equals(field);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_clrType);
        hash.Add(_inlineSize);
        hash.Add(_isInline);
        if (!_isInline)
        {
            hash.Add(_referenceValue);
        }
        else
        {
            for (int i = 0; i < _inlineSize; ++i)
                hash.Add(Unsafe.Add(ref Unsafe.AsRef(_inlineValue), i));
        }
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public static bool operator ==(DbfField left, DbfField right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(DbfField left, DbfField right) => !(left == right);

    private T ConvertTo<T>() where T : struct
    {
        switch (_clrType)
        {
            case ClrType.Boolean when typeof(T) == typeof(bool):
            case ClrType.Int32 when typeof(T) == typeof(int):
            case ClrType.Double when typeof(T) == typeof(double):
            case ClrType.DateTime when typeof(T) == typeof(DateTime):
            case ClrType.DateOnly when typeof(T) == typeof(DateOnly):
                break;
            default:
                throw new InvalidCastException();
        }

        return ReadInlineValue<T>();
    }

    private T? ConvertToNullable<T>() where T : struct
    {
        if (IsNull)
            return null;

        return ConvertTo<T>();
    }

    /// <summary>Performs an implicit conversion from <see cref="bool"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(bool value) => Logical(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField"/> to <see cref="bool"/>.</summary>
    public static explicit operator bool(DbfField value) => value.GetBoolean();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="bool" /><see langword="?" />.</summary>
    public static explicit operator bool?(DbfField value) => value.GetBooleanOrDefault();

    /// <summary>Performs an implicit conversion from <see cref="double"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(double value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double(DbfField value) => value.GetDouble();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double?(DbfField value) => value.GetDoubleOrDefault();

    /// <summary>Performs an implicit conversion from <see cref="DateTime" /> to <see cref="DbfField" />.</summary>
    public static implicit operator DbfField(DateTime value) => Timestamp(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime(DbfField value) => value.GetDateTime();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime?(DbfField value) => value.GetDateTimeOrDefault();

    /// <summary>Performs an implicit conversion from <see cref="DateOnly" /> to <see cref="DbfField" />.</summary>
    public static implicit operator DbfField(DateOnly value) => Date(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateOnly" /><see langword="?" />.</summary>
    public static explicit operator DateOnly(DbfField value) => value.GetDateOnly();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateOnly" /><see langword="?" />.</summary>
    public static explicit operator DateOnly?(DbfField value) => value.GetDateOnlyOrDefault();

    /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(string? value) => value is null ? Null(DbfFieldType.Character) : Character(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="string" />.</summary>
    public static explicit operator string?(DbfField value) => value.GetStringOrDefault();
}
