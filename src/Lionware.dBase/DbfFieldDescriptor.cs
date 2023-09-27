using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;

/// <summary>
/// Describes a <see cref="DbfField" />.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly struct DbfFieldDescriptor : IEquatable<DbfFieldDescriptor>
{
    private const int JulianOffsetToDateTime = 1721426;
    private static readonly DateTime DateTimeStart = new DateTime(1, 1, 1);

    [FieldOffset(0)]
    private readonly byte _name;
    [FieldOffset(10)]
    private readonly byte _zero; // '\0'
    [FieldOffset(11)]
    private readonly DbfFieldType _type;
    [FieldOffset(12)]
    private readonly int _address; // in memory address.
    [FieldOffset(16)]
    private readonly byte _length;
    [FieldOffset(17)]
    private readonly byte _decimal;
    [FieldOffset(20)]
    private readonly ushort _workAreaId;
    [FieldOffset(23)]
    private readonly int _setFields;
    [FieldOffset(31)]
    private readonly byte _inMdxFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldDescriptor"/> struct.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The type of the field.</param>
    /// <param name="length">The length of the field in bytes.</param>
    /// <param name="decimal">The number of characters allowed after the decimal separator.</param>
    public DbfFieldDescriptor(string name, DbfFieldType type, byte length, byte @decimal)
    {
        NameString = name;
        Type = type;
        Length = length;
        Decimal = @decimal;
    }

    /// <summary>
    /// Gets the field name in ASCII.
    /// </summary>
    public readonly ReadOnlySpan<byte> Name
    {
        get
        {
            unsafe
            {
                return MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _name)));
            }
        }
        init
        {
            var truncatedValue = value[..Math.Min(10, value.Length)];
            truncatedValue.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _name), 10));
        }
    }

    /// <summary>
    /// Gets the field name in UTF16.
    /// </summary>
    /// <remarks>
    /// Only ASCII characters are supported.
    /// </remarks>
    public readonly string NameString { get => Encoding.ASCII.GetString(Name); init => Name = Encoding.ASCII.GetBytes(value); }

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    public readonly DbfFieldType Type { get => _type; init => _type = value; }

    /// <summary>
    /// Gets or sets the length of the field in binary (maximum 254).
    /// </summary>
    public readonly byte Length { get => _length; init => _length = value; }

    /// <summary>
    /// Gets the field decimal count in binary.
    /// </summary>
    public readonly byte Decimal { get => _decimal; init => _decimal = value; }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfFieldDescriptor other)
    {
        return Equals(in _name, in other._name, 10)
            && _zero == other._zero
            && _type == other._type
            && _address == other._address
            && _length == other._length
            && _decimal == other._decimal
            && _workAreaId == other._workAreaId
            && _setFields == other._setFields
            && _inMdxFile == other._inMdxFile;

        static bool Equals(in byte left, in byte right, int length)
        {
            ref var leftRef = ref Unsafe.AsRef(left);
            ref var rightRef = ref Unsafe.AsRef(right);
            for (int i = 0; i < length; ++i)
            {
                ref var leftVal = ref Unsafe.Add(ref leftRef, i);
                ref var rightVal = ref Unsafe.Add(ref rightRef, i);
                if (leftVal != rightVal)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="Object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="Object" /> to compare with this instance.</param>
    /// <returns>
    ///   <see langword="true" /> if the specified <see cref="Object" /> is equal to this instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => obj is DbfFieldDescriptor descriptor && Equals(descriptor);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetHashCode(in _name, 10));
        hash.Add(_zero);
        hash.Add(_type);
        hash.Add(_address);
        hash.Add(_length);
        hash.Add(_decimal);
        hash.Add(_workAreaId);
        hash.Add(_setFields);
        hash.Add(_inMdxFile);
        return hash.ToHashCode();

        static int GetHashCode(in byte obj, int length)
        {
            var hash = new HashCode();
            ref var objRef = ref Unsafe.AsRef(obj);
            for (int i = 0; i < length; ++i)
            {
                ref var value = ref Unsafe.Add(ref objRef, i);
                hash.Add(value);
            }
            return hash.ToHashCode();
        }
    }

    internal readonly void CoerceLength()
    {
        switch (_type)
        {
            case DbfFieldType.Character:
                Unsafe.AsRef(in _length) = Clamp(_length, 1, 254);
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                Unsafe.AsRef(in _length) = Clamp(_length, 1, 254);
                Unsafe.AsRef(in _decimal) = Clamp(_decimal, 0, (byte)(_length - 1));
                break;
            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
            case DbfFieldType.Ole:
                Unsafe.AsRef(in _length) = 10;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfFieldType.Double:
            case DbfFieldType.Date:
            case DbfFieldType.Timestamp:
                Unsafe.AsRef(in _length) = 8;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfFieldType.Int32:
            case DbfFieldType.AutoIncrement:
                Unsafe.AsRef(in _length) = 4;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfFieldType.Logical:
                Unsafe.AsRef(in _length) = 1;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(_type), (int)_type, typeof(DbfFieldType));
        }

        static byte Clamp(byte val, byte min, byte max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }
    }

    internal readonly bool NameEquals(ReadOnlySpan<char> name)
    {
        if (name.Length <= 0 || name.Length > 10)
            return false;

        Span<char> buffer = stackalloc char[10];
        Encoding.ASCII.GetChars(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _name), 10), buffer);
        buffer = buffer.Trim('\0');
        return name.Equals(buffer, StringComparison.OrdinalIgnoreCase);
    }

    private string GetDebuggerDisplay() => $"{NameString}, {(char)_type}, {_length}, {_decimal}";

    internal DbfFieldMarshaller CreateMarshaller() => new(CreateReader(), CreateWriter());

    internal DbfFieldReader CreateReader()
    {
        var type = Type;
        var length = Length;
        var @decimal = Decimal;
        switch (Type)
        {
            case DbfFieldType.Character:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    return source.Length > 0 ? DbfField.Character(context.Encoding.GetString(source), length) : DbfField.Null(type, length, @decimal);
                };

            case DbfFieldType.Memo when Length is 4:
                return (source, context) =>
                {
                    if (context.MemoFile is null)
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Memo(context.MemoFile[MemoryMarshal.Read<int>(source)], length);
                };

            case DbfFieldType.Binary when Length is 4:
                return (source, context) =>
                {
                    if (context.MemoFile is null)
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Binary(context.MemoFile[MemoryMarshal.Read<int>(source)], length);
                };

            case DbfFieldType.Ole when Length is 4:
                return (source, context) =>
                {
                    if (context.MemoFile is null)
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Ole(context.MemoFile[MemoryMarshal.Read<int>(source)], length);
                };

            case DbfFieldType.Memo:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (context.MemoFile is null || source.Length == 0)
                        return DbfField.Null(type, length, @decimal);

                    Span<char> chars = stackalloc char[context.Encoding.GetMaxCharCount(source.Length)];
                    context.Encoding.GetChars(source, chars);
                    if (!int.TryParse(chars, out var index))
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Memo(context.MemoFile[index], length);
                };

            case DbfFieldType.Ole:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (context.MemoFile is null || source.Length == 0)
                        return DbfField.Null(type, length, @decimal);

                    Span<char> chars = stackalloc char[context.Encoding.GetMaxCharCount(source.Length)];
                    context.Encoding.GetChars(source, chars);
                    if (!int.TryParse(chars, out var index))
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Ole(context.MemoFile[index], length);
                };

            case DbfFieldType.Binary:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (context.MemoFile is null || source.Length == 0)
                        return DbfField.Null(type, length, @decimal);

                    Span<char> chars = stackalloc char[context.Encoding.GetMaxCharCount(source.Length)];
                    context.Encoding.GetChars(source, chars);
                    if (!int.TryParse(chars, out var index))
                        return DbfField.Null(type, length, @decimal);

                    return DbfField.Binary(context.MemoFile[index], length);
                };

            case DbfFieldType.Numeric:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    return source.Length > 0 ? DbfField.Numeric(Parse<double>(source, NumberStyles.Float, context), length, @decimal) : DbfField.Null(type, length, @decimal);
                };

            case DbfFieldType.Float:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    return source.Length > 0 ? DbfField.Float(Parse<double>(source, NumberStyles.Float, context), length, @decimal) : DbfField.Null(type, length, @decimal);
                };

            case DbfFieldType.Int32:
                return (source, context) => DbfField.Int32(MemoryMarshal.Read<int>(source));

            case DbfFieldType.AutoIncrement:
                return (source, context) => DbfField.AutoIncrement(MemoryMarshal.Read<int>(source));

            case DbfFieldType.Double:
                return (source, context) => DbfField.Double(MemoryMarshal.Read<double>(source));

            case DbfFieldType.Date:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (source.Length > 0)
                    {
                        Span<char> date = stackalloc char[8];
                        context.Encoding.GetChars(source[..8], date);
                        return DbfField.Date(DateOnly.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture));
                    }
                    return DbfField.Null(type, length, @decimal);
                };

            case DbfFieldType.Timestamp:
                return (source, context) => DbfField.Timestamp(DateTimeStart.AddDays(MemoryMarshal.Read<int>(source[..4]) - JulianOffsetToDateTime).AddMilliseconds(MemoryMarshal.Read<int>(source.Slice(4, 4))));

            case DbfFieldType.Logical:
                return (source, context) =>
                {
                    char logical = '\0';
                    context.Encoding.GetChars(source[..1], MemoryMarshal.CreateSpan(ref logical, 1));
                    return Char.ToUpperInvariant(logical) switch
                    {
                        'T' or 'Y' or '1' => DbfField.Logical(true),
                        'F' or 'N' or '0' => DbfField.Logical(false),
                        //'?' or ' '
                        _ => DbfField.Null(DbfFieldType.Logical, length, @decimal),
                    };
                };

            default:
                throw new InvalidEnumArgumentException(nameof(Type), (int)Type, typeof(DbfFieldType));
        }

        static T Parse<T>(ReadOnlySpan<byte> bytes, NumberStyles style, IDbfContext context) where T : INumberBase<T>
        {
            char[]? array = null;
            try
            {
                Span<char> chars = bytes.Length <= 64 ? stackalloc char[bytes.Length] : (array = ArrayPool<char>.Shared.Rent(bytes.Length)).AsSpan(0, bytes.Length);
                context.Encoding.GetChars(bytes, chars);
                if (context.DecimalSeparator is not '.' && chars.IndexOf(context.DecimalSeparator) is var index && index >= 0)
                    chars[index] = '.';
                return T.Parse(chars, style, CultureInfo.InvariantCulture);
            }
            finally
            {
                if (array is not null)
                    ArrayPool<char>.Shared.Return(array);
            }
        }
    }

    internal DbfFieldWriter CreateWriter()
    {
        var type = Type;
        var length = Length;
        var @decimal = Decimal;

        bool IsValidAndNotNull(in DbfField field, Span<byte> target)
        {
            if (type != field._dbfType)
                throw new InvalidOperationException($"Unexpected field type '{field._dbfType}'. Expected '{type}'");

            target.Fill((byte)' ');
            if (field.IsNull)
                return false;

            return true;
        }

        switch (Type)
        {
            case DbfFieldType.Character:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        ReadOnlySpan<char> @string = field.ReferenceValue;
                        @string = @string[..Math.Min(@string.Length, length)];
                        context.Encoding.GetBytes(@string, target);
                    }
                };

            case DbfFieldType.Ole when Length is 4:
            case DbfFieldType.Memo when Length is 4:
            case DbfFieldType.Binary when Length is 4:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (context.MemoFile is not null && IsValidAndNotNull(in field, target))
                    {
                        var index = context.MemoFile.Append(field.GetString());
                        MemoryMarshal.Write(target, ref index);
                    }
                };

            case DbfFieldType.Memo:
            case DbfFieldType.Binary:
            case DbfFieldType.Ole:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (context.MemoFile is not null && IsValidAndNotNull(in field, target))
                    {
                        var index = context.MemoFile.Append(field.GetString());
                        Span<char> chars = stackalloc char[10];
                        var result = index.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
                        Debug.Assert(result);
                        chars = chars[..charsWritten];
                        context.Encoding.GetBytes(chars, target[^chars.Length..]);
                    }
                };

            case DbfFieldType.Numeric:
            case DbfFieldType.Float:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                        FormatFloat<double>(in field, target, maxLengthToFormatT: 32, context, @decimal);
                };

            case DbfFieldType.Int32:
            case DbfFieldType.AutoIncrement:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        var i32 = field.ReadInlineValue<int>();
                        MemoryMarshal.Write(target, ref i32);
                    }
                };

            case DbfFieldType.Double:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        var f64 = field.ReadInlineValue<double>();
                        MemoryMarshal.Write(target, ref f64);
                    }
                };

            case DbfFieldType.Date:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        var date = field.ReadInlineValue<DateOnly>();
                        Span<char> chars = stackalloc char[8];
                        var result = date.TryFormat(chars, out _, "yyyyMMdd", CultureInfo.InvariantCulture);
                        Debug.Assert(result);
                        context.Encoding.GetBytes(chars, target);
                    }
                };

            case DbfFieldType.Timestamp:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        var timestamp = field.ReadInlineValue<DateTime>();
                        var timespan = timestamp - DateTimeStart;
                        var timestampDate = JulianOffsetToDateTime + (int)timespan.TotalDays;
                        MemoryMarshal.Write(target[..4], ref timestampDate);
                        var timestampTime = (int)(timespan - TimeSpan.FromDays(timespan.Days)).TotalMilliseconds;
                        MemoryMarshal.Write(target[4..], ref timestampTime);
                    }
                };

            case DbfFieldType.Logical:
                return (in DbfField field, Span<byte> target, IDbfContext context) =>
                {
                    if (IsValidAndNotNull(in field, target))
                    {
                        var boolean = field.ReadInlineValue<bool>();
                        target[0] = (byte)(boolean ? 'T' : 'F');
                    }
                    else
                    {
                        target[0] = (byte)'?';
                    }
                };

            default:
                throw new InvalidEnumArgumentException(nameof(Type), (int)Type, typeof(DbfFieldType));
        }

        static void FormatFloat<T>(in DbfField field, Span<byte> target, int maxLengthToFormatT, IDbfContext context, int decimalSpaces) where T : struct, IFloatingPoint<T>
        {
            Span<char> buffer = stackalloc char[maxLengthToFormatT];
            Span<char> format = GetFormat(stackalloc char[maxLengthToFormatT], decimalSpaces);
            var value = field.ReadInlineValue<T>();
            var result = value.TryFormat(buffer, out var charsWritten, format, CultureInfo.InvariantCulture);
            Debug.Assert(result);
            if (buffer.IndexOf('.') is var indexOfDot && indexOfDot >= 0)
                buffer[indexOfDot] = context.DecimalSeparator;
            if (charsWritten < target.Length)
            {
                buffer = buffer[..Math.Min(target.Length, buffer.Length)];
                var shift = target.Length - charsWritten;
                for (int i = target.Length - 1; i >= shift; --i)
                    buffer[i] = buffer[i - shift];
                buffer[..shift].Fill(' ');
            }
            else if (charsWritten > target.Length)
            {
                buffer = buffer[^target.Length..];
            }

            context.Encoding.GetBytes(buffer, target);

            static Span<char> GetFormat(Span<char> format, int decimalSpaces)
            {
                if (decimalSpaces <= 0)
                {
                    format = format[^2..];
                    format[0] = 'F';
                    format[1] = '0';
                }
                else
                {
                    var result = decimalSpaces.TryFormat(format[1..], out var charsWritten, "D0", CultureInfo.InvariantCulture);
                    Debug.Assert(result);
                    format = format[..(1 + charsWritten)];
                    format[0] = 'F';
                }

                return format;
            }
        }
    }

    /// <summary>
    /// Parses a string into a value.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The result of parsing <paramref name="s"/>.</returns>
    /// <exception cref="FormatException"></exception>
    public DbfField ParseField(string s)
    {
        if (!TryParseField(s, out var field))
            throw new FormatException();
        return field;
    }

    /// <summary>
    /// Tries to parse a string into a value.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing s or an
    /// undefined value on failure.</param>
    /// <returns><see langword="true" /> if s was successfully parsed; otherwise, <see langword="false" />.</returns>
    public bool TryParseField([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out DbfField result)
    {
        switch (Type)
        {
            case DbfFieldType.Character:
                result = DbfField.Character(s, Length);
                return true;

            case DbfFieldType.Memo:
                result = DbfField.Memo(s, Length);
                return true;

            case DbfFieldType.Binary:
                result = DbfField.Binary(s, Length);
                return true;

            case DbfFieldType.Ole:
                result = DbfField.Ole(s, Length);
                return true;

            case DbfFieldType _ when String.IsNullOrEmpty(s):
                result = DbfField.Null(Type, Length, Decimal);
                return true;

            case DbfFieldType.Numeric when double.TryParse(s, out var @double):
                result = DbfField.Numeric(@double, Length, Decimal);
                return true;

            case DbfFieldType.Float when double.TryParse(s, out var f64):
                result = DbfField.Float(f64, Length, Decimal);
                return true;

            case DbfFieldType.Double when double.TryParse(s, out var f64):
                result = DbfField.Double(f64);
                return true;

            case DbfFieldType.Int32 when int.TryParse(s, out var i32):
                result = DbfField.Int32(i32);
                return true;

            case DbfFieldType.AutoIncrement when int.TryParse(s, out var i32):
                result = DbfField.AutoIncrement(i32);
                return true;

            case DbfFieldType.Date when DateOnly.TryParseExact(s, "yyyyMMdd", out var date):
                result = DbfField.Date(date);
                return true;

            case DbfFieldType.Timestamp when DateTime.TryParse(s, out var timestamp):
                result = DbfField.Timestamp(timestamp);
                return true;

            case DbfFieldType.Logical:
                result = DbfField.Logical(s is "T" or "t" or "Y" or "y");
                return true;

            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Character" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Character(string name, byte length = 10)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Character,
            Length = length,
            Decimal = 0,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Date" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Date(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Date,
            Length = 8,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Float" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    /// <remarks>Identical to <see cref="DbfFieldType.Numeric" />; maintained for compatibility.</remarks>
    public static DbfFieldDescriptor Float(string name, byte length = 10, byte @decimal = 0)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Numeric,
            Length = length,
            Decimal = @decimal,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Numeric" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Numeric(string name, byte length = 10, byte @decimal = 0)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Numeric,
            Length = length,
            Decimal = @decimal,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Logical" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Logical(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Logical,
            Length = 1,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Timestamp" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Timestamp(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Timestamp,
            Length = 8,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Int32" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Int32(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Int32,
            Length = 4,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.AutoIncrement" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor AutoIncrement(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.AutoIncrement,
            Length = 4,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Double" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Double(string name)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Double,
            Length = 8,
            Decimal = 0
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Memo" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Memo(string name, byte length = 10)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Memo,
            Length = length,
            Decimal = 0,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Binary" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Binary(string name, byte length = 10)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Binary,
            Length = length,
            Decimal = 0,
        };
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfFieldType.Ole" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Ole(string name, byte length = 10)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        nameSpan = nameSpan[..10];
        return new()
        {
            Name = nameSpan,
            Type = DbfFieldType.Ole,
            Length = length,
            Decimal = 0,
        };
    }

    /// <summary>
    /// Implements the operator op_Equality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(DbfFieldDescriptor left, DbfFieldDescriptor right) => left.Equals(right);

    /// <summary>
    /// Implements the operator op_Inequality.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(DbfFieldDescriptor left, DbfFieldDescriptor right) => !(left == right);
}
