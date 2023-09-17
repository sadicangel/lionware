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
    // type code of the value.
    [FieldOffset(0)] private readonly byte _type;
    // dbf type of the value.
    [FieldOffset(1)] private readonly DbfFieldType _dbfType;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="type">The type of the field.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(DbfFieldType type, byte length, byte @decimal)
    {
        _inlineSize = sizeof(bool);
        _type = (byte)TypeCode.Empty;
        _dbfType = type;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(bool value, byte length = 1, byte @decimal = 0)
    {
        _inlineSize = sizeof(bool);
        _type = (byte)TypeCode.Boolean;
        _dbfType = DbfFieldType.Logical;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(byte value, byte length = 3, byte @decimal = 0)
    {
        _inlineSize = sizeof(byte);
        _type = (byte)TypeCode.Byte;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(short value, byte length = 5, byte @decimal = 0)
    {
        _inlineSize = sizeof(short);
        _type = (byte)TypeCode.Int16;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(int value, byte length = 10, byte @decimal = 0)
    {
        _inlineSize = sizeof(int);
        _type = (byte)TypeCode.Int32;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(long value, byte length = 19, byte @decimal = 0)
    {
        _inlineSize = sizeof(long);
        _type = (byte)TypeCode.Int64;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(float value, byte length = 14, byte @decimal = 7)
    {
        _inlineSize = sizeof(float);
        _type = (byte)TypeCode.Single;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(double value, byte length = 30, byte @decimal = 15)
    {
        _inlineSize = sizeof(double);
        _type = (byte)TypeCode.Double;
        _dbfType = DbfFieldType.Numeric;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(DateTime value, byte length = 8, byte @decimal = 0)
    {
        _inlineSize = (byte)Unsafe.SizeOf<DateTime>();
        _type = (byte)TypeCode.DateTime;
        _dbfType = DbfFieldType.Date;
        _isInline = true;
        _length = length;
        _decimal = @decimal;
        WriteInlineValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    public DbfField(string? value) : this(value: value, length: (byte)Math.Min(254, value?.Length ?? 0), @decimal: 0) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfField" /> struct.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <param name="length">The number of characters the field should have when stored.</param>
    /// <param name="decimal">The number of decimal characters the field should have when stored.</param>
    public DbfField(string? value, byte length, byte @decimal = 0)
    {
        _inlineSize = 0;
        _referenceValue = value;
        if (value is not null)
        {
            _type = (byte)TypeCode.String;
            if (value.Length > 254)
                _referenceValue = value[..254];
        }
        else
        {
            _type = (byte)TypeCode.Empty;
        }
        _dbfType = DbfFieldType.Character;
        _length = length;
        _decimal = @decimal;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is <see langword="null" />.
    /// </summary>
    public readonly bool IsNull { get => Type is TypeCode.Empty; }

    internal readonly TypeCode Type { get => (TypeCode)_type; }

    /// <summary>
    /// Gets the span stored in this instance, sliced to the correct size.
    /// If not an inline value, the span is empty.
    /// </summary>
    internal readonly ReadOnlySpan<byte> InlineValue { get => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _inlineValue), 8); }

    /// <summary>
    /// Gets the reference stored by this instance. The value is <see langword="null"/>
    /// ff type is <see cref="TypeCode.Empty"/> or not a reference value.
    /// </summary>
    internal readonly string? ReferenceValue { get => _referenceValue; }

    /// <summary>
    /// Gets the value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <remarks>The value is boxed for value types.</remarks>
    public object? Value
    {
        get => Type switch
        {
            TypeCode.Boolean => ReadInlineValue<bool>(),
            TypeCode.Byte => ReadInlineValue<byte>(),
            TypeCode.Int16 => ReadInlineValue<short>(),
            TypeCode.Int32 => ReadInlineValue<int>(),
            TypeCode.Int64 => ReadInlineValue<long>(),
            TypeCode.Single => ReadInlineValue<float>(),
            TypeCode.Double => ReadInlineValue<double>(),
            TypeCode.DateTime => ReadInlineValue<DateTime>(),
            _ => _referenceValue,
        };
    }

    /// <summary>
    /// Gets the value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <remarks>The value is boxed for value types.</remarks>
    public T? GetValue<T>() => (T?)Value;

    private void ThrowInvalidType(TypeCode expectedType) => throw new InvalidOperationException($"Cannot convert value to {expectedType}. Field type is {_type}");

    /// <summary>
    /// Gets the <see cref="bool"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public bool GetBoolean()
    {
        if (Type is not TypeCode.Boolean)
            ThrowInvalidType(TypeCode.Boolean);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetBoolean();
    }

    /// <summary>
    /// Gets the <see cref="byte"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public byte GetByte()
    {
        if (Type is not TypeCode.Byte)
            ThrowInvalidType(TypeCode.Byte);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetByte();
    }

    /// <summary>
    /// Gets the <see cref="short"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public short GetInt16()
    {
        if (Type is not TypeCode.Int16)
            ThrowInvalidType(TypeCode.Int16);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetInt16();
    }

    /// <summary>
    /// Gets the <see cref="int"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public int GetInt32()
    {
        if (Type is not TypeCode.Int32)
            ThrowInvalidType(TypeCode.Int32);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetInt32();
    }

    /// <summary>
    /// Gets the <see cref="long"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public long GetInt64()
    {
        if (Type is not TypeCode.Int64)
            ThrowInvalidType(TypeCode.Int64);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetInt64();
    }

    /// <summary>
    /// Gets the <see cref="float"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public float GetSingle()
    {
        if (Type is not TypeCode.Single)
            ThrowInvalidType(TypeCode.Single);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetSingle();
    }

    /// <summary>
    /// Gets the <see cref="double"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public double GetDouble()
    {
        if (Type is not TypeCode.Double)
            ThrowInvalidType(TypeCode.Double);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetDouble();
    }

    /// <summary>
    /// Gets the <see cref="DateTime"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public DateTime GetDateTime()
    {
        if (Type is not TypeCode.DateTime)
            ThrowInvalidType(TypeCode.DateTime);
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
        if (Type is TypeCode.Empty)
            return defaultValue;
        return GetDateTime();
    }

    /// <summary>
    /// Gets the <see cref="string"/> value of this <see cref="DbfRecord"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException" />
    public string GetString()
    {
        if (Type is not TypeCode.String)
            ThrowInvalidType(TypeCode.String);
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
        if (Type is TypeCode.Empty)
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
        if ((TypeCode)_type is TypeCode.Empty)
            return String.Empty;

        return _dbfType switch
        {
            DbfFieldType.Character => _referenceValue ?? String.Empty,
            DbfFieldType.Numeric or
            DbfFieldType.Float or
            DbfFieldType.Int32 or
            DbfFieldType.Double or
            DbfFieldType.AutoIncrement => Convert.ToDouble(Value).ToString($"F{_decimal}"),
            DbfFieldType.Date => GetDateTime().ToString("yyyyMMdd"),
            DbfFieldType.Timestamp => GetDateTime().ToString("HHmmss"),
            DbfFieldType.Logical => GetBoolean() ? "T" : "F",
            DbfFieldType.Memo => throw new NotImplementedException(),
            DbfFieldType.Binary => throw new NotImplementedException(),
            DbfFieldType.Ole => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    private string GetDebuggerDisplay() => ToString();

    internal T ReadInlineValue<T>() where T : struct => MemoryMarshal.Read<T>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(_inlineValue), _inlineSize));

    internal void WriteInlineValue<T>(T value) where T : struct => MemoryMarshal.Write(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(_inlineValue), _inlineSize), ref value);

    /// <inheritdoc />
    public bool Equals(DbfField other)
    {
        if (_type != other._type)
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
        hash.Add(_type);
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
        switch (Type)
        {
            case TypeCode.Boolean when typeof(T) == typeof(bool):
            case TypeCode.Int32 when typeof(T) == typeof(int):
            case TypeCode.Int64 when typeof(T) == typeof(long):
            case TypeCode.Double when typeof(T) == typeof(double):
            case TypeCode.DateTime when typeof(T) == typeof(DateTime):
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
    public static implicit operator DbfField(bool value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField"/> to <see cref="bool"/>.</summary>
    public static explicit operator bool(DbfField value) => value.ConvertTo<bool>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="bool" /><see langword="?" />.</summary>
    public static explicit operator bool?(DbfField value) => value.ConvertToNullable<bool>();

    /// <summary>Performs an implicit conversion from <see cref="byte"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(byte value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField"/> to <see cref="byte"/>.</summary>
    public static explicit operator byte(DbfField value) => value.ConvertTo<byte>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="byte" /><see langword="?" />.</summary>
    public static explicit operator byte?(DbfField value) => value.ConvertToNullable<byte>();

    /// <summary>Performs an implicit conversion from <see cref="short"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(short value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="short" />.</summary>
    public static explicit operator short(DbfField value) => value.ConvertTo<short>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="short" /><see langword="?" />.</summary>
    public static explicit operator short?(DbfField value) => value.ConvertToNullable<short>();

    /// <summary>Performs an implicit conversion from <see cref="int"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(int value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="int" />.</summary>
    public static explicit operator int(DbfField value) => value.ConvertTo<int>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="int" /><see langword="?" />.</summary>
    public static explicit operator int?(DbfField value) => value.ConvertToNullable<int>();

    /// <summary>Performs an implicit conversion from <see cref="long"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(long value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="long" />.</summary>
    public static explicit operator long(DbfField value) => value.ConvertTo<long>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="long" /><see langword="?" />.</summary>
    public static explicit operator long?(DbfField value) => value.ConvertToNullable<long>();

    /// <summary>Performs an implicit conversion from <see cref="float"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(float value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="float" /><see langword="?" />.</summary>
    public static explicit operator float(DbfField value) => value.ConvertTo<float>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="float" /><see langword="?" />.</summary>
    public static explicit operator float?(DbfField value) => value.ConvertToNullable<float>();

    /// <summary>Performs an implicit conversion from <see cref="double"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(double value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double(DbfField value) => value.ConvertTo<double>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="double" /><see langword="?" />.</summary>
    public static explicit operator double?(DbfField value) => value.ConvertToNullable<double>();

    /// <summary>Performs an implicit conversion from <see cref="DateTime" /> to <see cref="DbfField" />.</summary>
    public static implicit operator DbfField(DateTime value) => new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime(DbfField value) => value.ConvertTo<DateTime>();
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="DateTime" /><see langword="?" />.</summary>
    public static explicit operator DateTime?(DbfField value) => value.ConvertToNullable<DateTime>();

    /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="DbfField"/>.</summary>
    public static implicit operator DbfField(string? value) => value is null ? new() : new(value);
    /// <summary>Performs an explicit conversion from <see cref="DbfField" /> to <see cref="string" />.</summary>
    public static explicit operator string?(DbfField value) => value._referenceValue;
}
