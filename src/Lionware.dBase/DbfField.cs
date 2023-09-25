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
        Byte = 6,
        Int16 = 7,
        Int32 = 9,
        Int64 = 11,
        Single = 13,
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
            ClrType.Byte => ReadInlineValue<byte>(),
            ClrType.Int16 => ReadInlineValue<short>(),
            ClrType.Int32 => ReadInlineValue<int>(),
            ClrType.Int64 => ReadInlineValue<long>(),
            ClrType.Single => ReadInlineValue<float>(),
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

    private void ThrowInvalidType(ClrType expectedType) => throw new InvalidOperationException($"Cannot convert value to {expectedType}. Field type is {_clrType}");

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
    public static DbfField Float(float value, byte length = 10, byte @decimal = 0) => CreateInline(value, ClrType.Single, DbfFieldType.Float, length, @decimal);

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
    public static DbfField Numeric(byte value, byte length = 3, byte @decimal = 0) => CreateInline(value, ClrType.Byte, DbfFieldType.Numeric, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Numeric(short value, byte length = 5, byte @decimal = 0) => CreateInline(value, ClrType.Int16, DbfFieldType.Numeric, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Numeric(int value, byte length = 10, byte @decimal = 0) => CreateInline(value, ClrType.Int32, DbfFieldType.Numeric, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Numeric(long value, byte length = 20, byte @decimal = 0) => CreateInline(value, ClrType.Int64, DbfFieldType.Numeric, length, @decimal);

    /// <summary>
    /// Creates a new <see cref="DbfField" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="value">The value of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new <see cref="DbfField" /> that stores the specified <paramref name="value"/>.
    /// </returns>
    public static DbfField Numeric(float value, byte length = 14, byte @decimal = 7) => CreateInline(value, ClrType.Single, DbfFieldType.Numeric, length, @decimal);

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
    public bool GetBoolean()
    {
        if (_clrType is not ClrType.Boolean)
            ThrowInvalidType(ClrType.Boolean);
        return ReadInlineValue<bool>();
    }

    /// <summary>
    /// Gets the <see cref="bool"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public bool GetBooleanOrDefault(bool defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetBoolean();
    }

    /// <summary>
    /// Gets the <see cref="byte"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public byte GetByte()
    {
        if (_clrType is not ClrType.Byte)
            ThrowInvalidType(ClrType.Byte);
        return ReadInlineValue<byte>();
    }

    /// <summary>
    /// Gets the <see cref="byte"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public byte GetByteOrDefault(byte defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetByte();
    }

    /// <summary>
    /// Gets the <see cref="short"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public short GetInt16()
    {
        if (_clrType is not ClrType.Int16)
            ThrowInvalidType(ClrType.Int16);
        return ReadInlineValue<short>();
    }

    /// <summary>
    /// Gets the <see cref="short"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public short GetInt16OrDefault(short defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetInt16();
    }

    /// <summary>
    /// Gets the <see cref="int"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public int GetInt32()
    {
        if (_clrType is not ClrType.Int32)
            ThrowInvalidType(ClrType.Int32);
        return ReadInlineValue<int>();
    }

    /// <summary>
    /// Gets the <see cref="int"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public int GetInt32OrDefault(int defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetInt32();
    }

    /// <summary>
    /// Gets the <see cref="long"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public long GetInt64()
    {
        if (_clrType is not ClrType.Int64)
            ThrowInvalidType(ClrType.Int64);
        return ReadInlineValue<long>();
    }

    /// <summary>
    /// Gets the <see cref="long"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public long GetInt64OrDefault(long defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetInt64();
    }

    /// <summary>
    /// Gets the <see cref="float"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public float GetSingle()
    {
        if (_clrType is not ClrType.Single)
            ThrowInvalidType(ClrType.Single);
        return ReadInlineValue<float>();
    }

    /// <summary>
    /// Gets the <see cref="float"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public float GetSingleOrDefault(float defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetSingle();
    }

    /// <summary>
    /// Gets the <see cref="double"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public double GetDouble()
    {
        if (_clrType is not ClrType.Double)
            ThrowInvalidType(ClrType.Double);
        return ReadInlineValue<double>();
    }

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
    public DateTime GetDateTime()
    {
        if (_clrType is not ClrType.DateTime)
            ThrowInvalidType(ClrType.DateTime);
        return ReadInlineValue<DateTime>();
    }

    /// <summary>
    /// Gets the <see cref="DateTime"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public DateTime GetDateTimeOrDefault(DateTime defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetDateTime();
    }

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public DateOnly GetDateOnly()
    {
        if (_clrType is not ClrType.DateOnly)
            ThrowInvalidType(ClrType.DateOnly);
        return ReadInlineValue<DateOnly>();
    }

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    public DateOnly GetDateOnlyOrDefault(DateOnly defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetDateOnly();
    }

    /// <summary>
    /// Gets the <see cref="string"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public string GetString()
    {
        if (_clrType is not ClrType.String)
            ThrowInvalidType(ClrType.String);
        return _referenceValue!;
    }

    /// <summary>
    /// Gets the <see cref="string"/> value of this <see cref="DbfRecord"/>
    /// or the specified <paramref name="defaultValue"/> if the field is empty.
    /// </summary>
    /// <param name="defaultValue">The default value to return if the field is empty.</param>
    /// <exception cref="InvalidOperationException" />
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? GetStringOrDefault(string? defaultValue = default)
    {
        if (_clrType is ClrType.Empty)
            return defaultValue;
        return GetString();
    }

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
            case ClrType.Byte when typeof(T) == typeof(byte):
            case ClrType.Int16 when typeof(T) == typeof(short):
            case ClrType.Int32 when typeof(T) == typeof(int):
            case ClrType.Int64 when typeof(T) == typeof(long):
            case ClrType.Single when typeof(T) == typeof(float):
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
    public static explicit operator bool(DbfField value) => value.ConvertTo<bool>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="bool" /><see langword="?" />.</summary>
    public static explicit operator bool?(DbfField value) => value.ConvertToNullable<bool>();

    /// <summary>Performs an implicit conversion from <see cref="byte"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(byte value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField"/> to <see cref="byte"/>.</summary>
    public static explicit operator byte(DbfField value) => value.ConvertTo<byte>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="byte" /><see langword="?" />.</summary>
    public static explicit operator byte?(DbfField value) => value.ConvertToNullable<byte>();

    /// <summary>Performs an implicit conversion from <see cref="short"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(short value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="short" />.</summary>
    public static explicit operator short(DbfField value) => value.ConvertTo<short>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="short" /><see langword="?" />.</summary>
    public static explicit operator short?(DbfField value) => value.ConvertToNullable<short>();

    /// <summary>Performs an implicit conversion from <see cref="int"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(int value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="int" />.</summary>
    public static explicit operator int(DbfField value) => value.ConvertTo<int>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="int" /><see langword="?" />.</summary>
    public static explicit operator int?(DbfField value) => value.ConvertToNullable<int>();

    /// <summary>Performs an implicit conversion from <see cref="long"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(long value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="long" />.</summary>
    public static explicit operator long(DbfField value) => value.ConvertTo<long>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="long" /><see langword="?" />.</summary>
    public static explicit operator long?(DbfField value) => value.ConvertToNullable<long>();

    /// <summary>Performs an implicit conversion from <see cref="float"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(float value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="float" /><see langword="?" />.</summary>
    public static explicit operator float(DbfField value) => value.ConvertTo<float>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="float" /><see langword="?" />.</summary>
    public static explicit operator float?(DbfField value) => value.ConvertToNullable<float>();

    /// <summary>Performs an implicit conversion from <see cref="double"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(double value) => Numeric(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double(DbfField value) => value.ConvertTo<double>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double?(DbfField value) => value.ConvertToNullable<double>();

    /// <summary>Performs an implicit conversion from <see cref="DateTime" /> to <see cref="DbfField" />.</summary>
    public static implicit operator DbfField(DateTime value) => Timestamp(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime(DbfField value) => value.ConvertTo<DateTime>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime?(DbfField value) => value.ConvertToNullable<DateTime>();

    /// <summary>Performs an implicit conversion from <see cref="DateOnly" /> to <see cref="DbfField" />.</summary>
    public static implicit operator DbfField(DateOnly value) => Date(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateOnly" /><see langword="?" />.</summary>
    public static explicit operator DateOnly(DbfField value) => value.ConvertTo<DateOnly>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateOnly" /><see langword="?" />.</summary>
    public static explicit operator DateOnly?(DbfField value) => value.ConvertToNullable<DateOnly>();

    /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(string? value) => value is null ? Null(DbfFieldType.Character) : Character(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="string" />.</summary>
    public static explicit operator string?(DbfField value) => value._referenceValue;
}
