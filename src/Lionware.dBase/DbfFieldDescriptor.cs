﻿using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;

internal delegate object? DbfFieldReader(ReadOnlySpan<byte> source, IDbfContext context);
internal delegate void DbfFieldWriter(object? field, Span<byte> target, IDbfContext context);

/// <summary>
/// Describes a field in a <see cref="Dbf"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 32)]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly struct DbfFieldDescriptor : IEquatable<DbfFieldDescriptor>
{
    private const int MaxNameLength = 11;
    private const int JulianOffsetToDateTime = 1721426;
    private static readonly DateTime DateTimeStart = new(1, 1, 1);
    private static readonly string[] DateFormats = new string[]
    {
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyyTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fffffffK",
    };

    [FieldOffset(0)]
    private readonly byte _name;
    [FieldOffset(11)]
    private readonly DbfType _type;
    [FieldOffset(12)]
    private readonly uint _address;
    [FieldOffset(16)]
    private readonly ushort _lengthU16;
    [FieldOffset(16)]
    private readonly byte _length;
    [FieldOffset(17)]
    private readonly byte _decimal;
    // 18-19 reserved for dBASE IIIPlus/LAN
    [FieldOffset(20)]
    private readonly byte _workAreaId;
    // 21-22 reserved for dBASE IIIPlus/LAN
    [FieldOffset(23)]
    private readonly byte _setFields;
    // 24-30 reserved
    [FieldOffset(31)]
    private readonly byte _inMdxFile;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldDescriptor"/> struct.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The type of the field.</param>
    /// <param name="length">The length of the field in bytes.</param>
    /// <param name="decimal">The number of characters allowed after the decimal separator.</param>
    public DbfFieldDescriptor(ReadOnlySpan<char> name, DbfType type, byte length, byte @decimal)
    {
        var chars = name[..Math.Min(name.Length, MaxNameLength)];
        var bytes = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _name), MaxNameLength);
        bytes.Clear();
        Encoding.ASCII.GetBytes(chars, bytes);
        _type = type;
        _length = length;
        _decimal = @decimal;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfFieldDescriptor"/> struct.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="type">The type of the field.</param>
    /// <param name="length">The length of the field in bytes.</param>
    /// <param name="decimal">The number of characters allowed after the decimal separator.</param>
    public DbfFieldDescriptor(ReadOnlySpan<byte> name, DbfType type, byte length, byte @decimal)
    {
        var bytes = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _name), MaxNameLength);
        bytes.Clear();
        name[..Math.Min(name.Length, MaxNameLength)].CopyTo(bytes);
        _type = type;
        _length = length;
        _decimal = @decimal;
    }

    /// <summary>
    /// Gets the field name in ASCII.
    /// </summary>
    public readonly ReadOnlySpan<byte> NameAscii
    {
        get
        {
            unsafe
            {
                return MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in _name)));
            }
        }
    }

    /// <summary>
    /// Gets the field name in UTF16.
    /// </summary>
    /// <remarks>
    /// Only ASCII characters are supported.
    /// </remarks>
    public readonly string Name { get => Encoding.ASCII.GetString(NameAscii); }

    /// <summary>
    /// Gets the type of the field.
    /// </summary>
    public readonly DbfType Type { get => _type; }

    /// <summary>
    /// Gets or sets the length of the field in bytes.
    /// </summary>
    public readonly ushort Length { get => Type is DbfType.Character ? _lengthU16 : _length; }

    /// <summary>
    /// Gets the field decimal count in binary.
    /// </summary>
    public readonly byte Decimal { get => Type is DbfType.Character ? (byte)0 : _decimal; }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfFieldDescriptor other)
    {
        return Equals(in _name, in other._name, MaxNameLength)
            && _type == other._type
            && _length == other._length
            && _decimal == other._decimal;

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
        hash.Add(GetHashCode(in _name, MaxNameLength));
        hash.Add(_type);
        hash.Add(_length);
        hash.Add(_decimal);
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
            case DbfType.Character:
                Unsafe.AsRef(in _length) = Clamp(_length, 1, 254);
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfType.Numeric:
            case DbfType.Float:
                Unsafe.AsRef(in _length) = Clamp(_length, 1, 254);
                Unsafe.AsRef(in _decimal) = Clamp(_decimal, 0, (byte)(_length - 1));
                break;
            case DbfType.Memo:
            case DbfType.Binary:
            case DbfType.Ole:
                Unsafe.AsRef(in _length) = 10;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfType.Double:
            case DbfType.Date:
            case DbfType.Timestamp:
                Unsafe.AsRef(in _length) = 8;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfType.Int32:
            case DbfType.AutoIncrement:
                Unsafe.AsRef(in _length) = 4;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            case DbfType.Logical:
                Unsafe.AsRef(in _length) = 1;
                Unsafe.AsRef(in _decimal) = 0;
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(_type), (int)_type, typeof(DbfType));
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

    private string GetDebuggerDisplay() => $"{Name}, {(char)_type}, {_length}, {_decimal}";

    internal DbfFieldReader CreateReader()
    {
        var type = Type;
        var length = Length;
        var @decimal = Decimal;
        switch (Type)
        {
            case DbfType.Character:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    return source.Length > 0 ? context.Encoding.GetString(source) : null;
                };

            case DbfType.Memo when Length is 4:
            case DbfType.Binary when Length is 4:
                return (source, context) =>
                {
                    if (context.MemoFile is null)
                        return null;

                    return context.MemoFile[BinaryPrimitives.ReadInt32LittleEndian(source)];
                };

            case DbfType.Memo:
            case DbfType.Binary:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (context.MemoFile is null || source.Length == 0)
                        return null;

                    if (!TryParseInt32(source, context.Encoding, out var index))
                        return null;

                    return context.MemoFile[index];
                };

            case DbfType.Ole:
                return (source, context) => null;

            case DbfType.Numeric:
            case DbfType.Float:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    return source.Length > 0 ? ParseF64(source, NumberStyles.Float, context) : null;
                };

            case DbfType.Int32:
            case DbfType.AutoIncrement:
                return (source, context) => BinaryPrimitives.ReadInt32LittleEndian(source);

            case DbfType.Double:
                return (source, context) => BinaryPrimitives.ReadDoubleLittleEndian(source);

            case DbfType.Currency:
                return (source, context) => (decimal)BinaryPrimitives.ReadDoubleLittleEndian(source);

            case DbfType.Date:
                return (source, context) =>
                {
                    source = source.Trim("\0 "u8);
                    if (source.Length > 0)
                    {
                        Span<char> date = stackalloc char[8];
                        context.Encoding.GetChars(source[..8], date);
                        return DateOnly.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
                    }
                    return null;
                };

            case DbfType.Timestamp:
                return (source, context) => DateTimeStart.AddDays(BinaryPrimitives.ReadInt32LittleEndian(source[..4]) - JulianOffsetToDateTime).AddMilliseconds(BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4, 4)));

            case DbfType.Logical:
                return (source, context) =>
                {
                    char logical = '\0';
                    context.Encoding.GetChars(source[..1], MemoryMarshal.CreateSpan(ref logical, 1));
                    return Char.ToUpperInvariant(logical) switch
                    {
                        'T' or 'Y' or '1' => true,
                        'F' or 'N' or '0' => false,
                        //'?' or ' '
                        _ => null,
                    };
                };


            case DbfType.NullFlags:
                return (source, context) => context.Encoding.GetString(source);

            default:
                return (source, context) => null;
        }

        static bool TryParseInt32(ReadOnlySpan<byte> source, Encoding encoding, out int i32)
        {
            var length = encoding.GetMaxCharCount(source.Length);
            char[]? array = null;
            try
            {
                Span<char> buffer = length < 64
                    ? stackalloc char[length]
                    : (array = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);

                encoding.GetChars(source, buffer);
                var result = int.TryParse(buffer, out i32);
                return result;
            }
            finally
            {
                if (array is not null)
                    ArrayPool<char>.Shared.Return(array);
            }
        }

        static double ParseF64(ReadOnlySpan<byte> bytes, NumberStyles style, IDbfContext context)
        {
            char[]? array = null;
            try
            {
                Span<char> buffer = bytes.Length < 64
                    ? stackalloc char[bytes.Length]
                    : (array = ArrayPool<char>.Shared.Rent(bytes.Length)).AsSpan(0, bytes.Length);

                context.Encoding.GetChars(bytes, buffer);
                return double.Parse(buffer, style, CultureInfo.InvariantCulture); ;
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

        static bool IsValidAndNotNull([NotNullWhen(true)] object? field, Span<byte> target)
        {
            target.Fill((byte)' ');
            return field is not null;
        }

        switch (Type)
        {
            case DbfType.Character:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                    {
                        ReadOnlySpan<char> @string = (string)field;
                        @string = @string[..Math.Min(@string.Length, length)];
                        context.Encoding.GetBytes(@string, target);
                    }
                };

            case DbfType.Memo when Length is 4:
            case DbfType.Binary when Length is 4:
                return (field, target, context) =>
                {
                    if (context.MemoFile is not null && IsValidAndNotNull(field, target))
                    {
                        var index = context.MemoFile.Append((string)field);
                        BinaryPrimitives.WriteInt32LittleEndian(target, index);
                    }
                };

            case DbfType.Memo:
            case DbfType.Binary:
                return (field, target, context) =>
                {
                    if (context.MemoFile is not null && IsValidAndNotNull(field, target))
                    {
                        var index = context.MemoFile.Append((string)field);
                        Span<char> chars = stackalloc char[10];
                        var result = index.TryFormat(chars, out var charsWritten, default, CultureInfo.InvariantCulture);
                        Debug.Assert(result);
                        chars = chars[..charsWritten];
                        context.Encoding.GetBytes(chars, target[^chars.Length..]);
                    }
                };

            case DbfType.Ole:
                return (field, target, context) => _ = IsValidAndNotNull(field, target);

            case DbfType.Numeric:
            case DbfType.Float:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                        FormatF64((double)field, target, context, @decimal);
                };

            case DbfType.Int32:
            case DbfType.AutoIncrement:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                        BinaryPrimitives.WriteInt32LittleEndian(target, (int)field);
                };

            case DbfType.Double:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                        BinaryPrimitives.WriteDoubleLittleEndian(target, (double)field);
                };

            case DbfType.Currency:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                        BinaryPrimitives.WriteDoubleLittleEndian(target, (double)(decimal)field);
                };

            case DbfType.Date:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                    {
                        Span<char> chars = stackalloc char[8];
                        var result = ((DateOnly)field).TryFormat(chars, out _, "yyyyMMdd", CultureInfo.InvariantCulture);
                        Debug.Assert(result);
                        context.Encoding.GetBytes(chars, target);
                    }
                };

            case DbfType.Timestamp:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                    {
                        var timestamp = (DateTime)field;
                        var timespan = timestamp - DateTimeStart;
                        var timestampDate = JulianOffsetToDateTime + (int)timespan.TotalDays;
                        BinaryPrimitives.WriteInt32LittleEndian(target[..4], timestampDate);
                        var timestampTime = (int)(timespan - TimeSpan.FromDays(timespan.Days)).TotalMilliseconds;
                        BinaryPrimitives.WriteInt32LittleEndian(target[4..], timestampTime);
                    }
                };

            case DbfType.Logical:
                return (field, target, context) => target[0] = IsValidAndNotNull(field, target) ? (byte)((bool)field ? 'T' : 'F') : (byte)'?';

            case DbfType.NullFlags:
                return (field, target, context) =>
                {
                    if (IsValidAndNotNull(field, target))
                    {
                        ReadOnlySpan<char> @string = (string)field;
                        @string = @string[..Math.Min(@string.Length, length)];
                        context.Encoding.GetBytes(@string, target);
                    }
                };


            default:
                return (field, target, context) => _ = IsValidAndNotNull(field, target);
        }

        static void FormatF64(double value, Span<byte> target, IDbfContext context, int decimalSpaces)
        {
            const int maxLengthToFormatT = 32;
            Span<char> buffer = stackalloc char[maxLengthToFormatT];
            Span<char> format = GetFormat(stackalloc char[maxLengthToFormatT], decimalSpaces);
            if (!value.TryFormat(buffer, out var charsWritten, format, CultureInfo.InvariantCulture))
            {
                var array = ArrayPool<char>.Shared.Rent(512);
                if (!value.TryFormat(array, out charsWritten, format, CultureInfo.InvariantCulture))
                {
                    Debug.WriteLine($"Failed to format {value}");
                    return;
                }
                charsWritten = Math.Min(maxLengthToFormatT, charsWritten);
                array.AsSpan(^charsWritten..).CopyTo(buffer);
            }

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
            else
            {
                buffer = buffer[..charsWritten];
            }

            // Replace decimal separator.
            if (context.DecimalSeparator is not '.' && buffer.IndexOf('.') is var indexOfDot && indexOfDot >= 0)
                buffer[indexOfDot] = context.DecimalSeparator;

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
    public object? ParseField(string s)
    {
        if (!TryParseField(s, out var field))
            throw new FormatException($"Value '{s}' could not be parsed into a valid '{Type}' value");
        return field;
    }

    /// <summary>
    /// Tries to parse a string into a value.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing s or an
    /// undefined value on failure.</param>
    /// <returns><see langword="true" /> if s was successfully parsed; otherwise, <see langword="false" />.</returns>
    public bool TryParseField([NotNullWhen(true)] string? s, out object? result)
    {
        result = null;
        if (String.IsNullOrEmpty(s))
            return true;

        switch (Type)
        {
            case DbfType.Character:
            case DbfType.Memo:
            case DbfType.Binary:
                result = s;
                return true;

            case DbfType.Ole:
                return false;

            case DbfType.Numeric when double.TryParse(s, CultureInfo.InvariantCulture, out var f64):
            case DbfType.Float when double.TryParse(s, CultureInfo.InvariantCulture, out f64):
            case DbfType.Double when double.TryParse(s, CultureInfo.InvariantCulture, out f64):
                result = f64;
                return true;

            case DbfType.Int32 when int.TryParse(s, CultureInfo.InvariantCulture, out var i32):
            case DbfType.AutoIncrement when int.TryParse(s, CultureInfo.InvariantCulture, out i32):
                result = i32;
                return true;

            case DbfType.Date when DateOnly.TryParseExact(s, "yyyyMMdd", out var date):
                result = date;
                return true;

            case DbfType.Timestamp when DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp):
                result = timestamp;
                return true;

            case DbfType.Logical:
                result = s is "T" or "t" or "Y" or "y";
                return true;

            case DbfType.Currency when decimal.TryParse(s, CultureInfo.InvariantCulture, out var d128):
                result = d128;
                return true;

            case DbfType.NullFlags:
                result = s;
                return true;

            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Character" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Character(string name, ushort length = 10)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref length, 1));
        return new(nameSpan, DbfType.Character, length: bytes[0], @decimal: bytes[1]);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Date" />.
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
        return new(nameSpan, DbfType.Date, 8, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Float" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field (number of ASCII characters).</param>
    /// <param name="decimal">The number of decimal digits.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    /// <remarks>Identical to <see cref="DbfType.Numeric" />; maintained for compatibility.</remarks>
    public static DbfFieldDescriptor Float(string name, byte length = 10, byte @decimal = 0)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        return new(nameSpan, DbfType.Float, length, @decimal);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Numeric" />.
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
        return new(nameSpan, DbfType.Numeric, length, @decimal);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Logical" />.
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
        return new(nameSpan, DbfType.Logical, 1, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Timestamp" />.
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
        return new(nameSpan, DbfType.Timestamp, 8, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Int32" />.
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
        return new(nameSpan, DbfType.Int32, 4, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.AutoIncrement" />.
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
        return new(nameSpan, DbfType.AutoIncrement, 4, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Double" />.
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
        return new(nameSpan, DbfType.Double, 8, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Currency" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field in bytes.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor Currency(string name, byte length = 8)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        return new(nameSpan, DbfType.Currency, length, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Memo" />.
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
        return new(nameSpan, DbfType.Memo, length, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Binary" />.
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
        return new(nameSpan, DbfType.Binary, length, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.Ole" />.
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
        return new(nameSpan, DbfType.Ole, length, 0);
    }

    /// <summary>
    /// Creates a new <see cref="DbfFieldDescriptor" /> of type <see cref="DbfType.NullFlags" />.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="length">The length of the field in bytes.</param>
    /// <returns>
    /// A new instance of <see cref="DbfFieldDescriptor" />
    /// </returns>
    public static DbfFieldDescriptor NullFlags(string name, byte length)
    {
        Span<byte> nameSpan = stackalloc byte[20];
        nameSpan.Clear();
        Encoding.ASCII.GetBytes(name, nameSpan);
        return new(nameSpan, DbfType.NullFlags, length, 0);
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
