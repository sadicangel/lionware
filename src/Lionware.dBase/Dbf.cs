using Lionware.IO;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Lionware.dBase;

/// <summary>
/// Represents a dBase file.
/// </summary>
/// <seealso cref="IDisposable" />
/// <seealso cref="ICollection{T}" />
public sealed class Dbf : IDbfContext, IDisposable, IEnumerable<DbfRecord>
{
    private const int EofByte = 0x1A;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Stream _stream;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte _version = 3;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DbfRecordDescriptor _recordDescriptor;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Encoding? _encoding;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private char? _decimalSeparator;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dbf"/> class from an existing file.
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    public Dbf(string fileName)
    {
        Ensure.NotNullOrEmpty(fileName);

        FileName = fileName;

        _stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);

        // Read the metadata.
        using var reader = new BinaryReader(_stream, Encoding.ASCII, leaveOpen: true);
        _version = reader.ReadByte();
        LastUpdate = new DateOnly(year: 1900 + reader.ReadByte(), month: reader.ReadByte(), day: reader.ReadByte());
        RecordCount = reader.ReadInt32();
        var headerLength = reader.ReadInt16();
        var recordLength = reader.ReadInt16();
        reader.BaseStream.Position += 2; // Reserved.
        InTransaction = reader.ReadByte() == 1;
        IsEncrypted = reader.ReadByte() == 1;
        reader.BaseStream.Position += 12; // Reserved.
        HasMdxFile = reader.ReadByte() == 1;
        Language = (DbfLanguage)reader.ReadByte();
        reader.BaseStream.Position += 2; // Reserved.

        var descriptors = new DbfFieldDescriptor[headerLength / 32 - 1];
        reader.Read(MemoryMarshal.AsBytes(descriptors.AsSpan()));
        _recordDescriptor = new DbfRecordDescriptor(descriptors);

        if (reader.ReadByte() != 0x0D)
            throw new FormatException("Not a dBase file");

        if (HeaderLength != headerLength)
            throw new InvalidOperationException("Invalid header length");

        if (RecordLength != recordLength)
            throw new InvalidOperationException("Invalid record length");

        _stream.Position = 0;

        if (HasDbtMemo && DbfMemoFile.TryOpen(this, out var memoFile))
            MemoFile = memoFile;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Dbf"/> class by creating a new file.
    /// </summary>
    /// <param name="fileName">Name of the file to create.</param>
    /// <param name="recordDescriptor">The record descriptor.</param>
    public Dbf(string fileName, DbfRecordDescriptor recordDescriptor)
    {
        Ensure.NotNullOrEmpty(fileName);

        FileName = fileName;

        _stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
        _recordDescriptor = recordDescriptor;

        _version = 0x03;
        if (recordDescriptor.FieldDescriptors.Any(f => f.Type is DbfFieldType.Binary or DbfFieldType.Ole or DbfFieldType.Memo))
            _version |= 0x80;

        // Write the metadata.
        using var writer = new BinaryWriter(_stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(_version);
        writer.Write((byte)(LastUpdate.Year - 1900));
        writer.Write((byte)LastUpdate.Month);
        writer.Write((byte)LastUpdate.Day);
        writer.Write(RecordCount);
        writer.Write(HeaderLength);
        writer.Write(RecordLength);
        writer.Write((short)0);
        writer.Write(Convert.ToByte(InTransaction));
        writer.Write(Convert.ToByte(IsEncrypted));
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(Convert.ToByte(HasMdxFile));
        writer.Write((byte)Language);
        writer.Write((short)0);
        writer.Write(MemoryMarshal.AsBytes(_recordDescriptor.FieldDescriptors));
        writer.Write(0x0D);
        // Write the EOF byte (0x1A).
        // This byte is overwritten when Add/AddRange/Clear is called. So these methods must append the byte again.
        // For insert/remove, since the data is shifted right/left, the byte is kept.
        writer.Write((byte)EofByte);

        _stream.Position = 0;

        if (HasDbtMemo)
            MemoFile = DbfMemoFile.Create(this);
    }

    /// <summary>
    /// Gets the length of the inner stream.
    /// </summary>
    internal long Length { get => _stream.Length; }

    /// <summary>
    /// Gets or sets the inner stream position;
    /// </summary>
    internal long Position { get => _stream.Position; set => _stream.Position = value; }

    /// <summary>
    /// Gets the file name of this <see cref="Dbf"/>.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the dBase version number.
    /// </summary>
    public int Version { get => _version/* & 0x07; init => _version = (byte)(_version & 0xF8 | value & 0x07)*/; }

    /// <summary>
    /// Gets the dBase version description.
    /// </summary>
    public string VersionDescription
    {
        get => _version switch
        {
            0x02 => "FoxPro",
            0x03 => "dBase III without memo file",
            0x04 => "dBase IV without memo file",
            0x05 => "dBase V without memo file",
            0x07 => "Visual Objects 1.x",
            0x30 => "Visual FoxPro",
            0x31 => "Visual FoxPro with AutoIncrement field",
            0x43 => "dBASE IV SQL table files, no memo",
            0x63 => "dBASE IV SQL system files, no memo",
            0x7b => "dBase IV with memo file",
            0x83 => "dBase III with memo file",
            0x87 => "Visual Objects 1.x with memo file",
            0x8b => "dBase IV with memo file",
            0x8e => "dBase IV with SQL table",
            0xcb => "dBASE IV SQL table files, with memo",
            0xf5 => "FoxPro with memo file",
            0xfb => "FoxPro without memo file",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Gets a value indicating whether this instance is a FoxPro format.
    /// </summary>
    public bool IsFoxPro { get => Version is 0x30 or 0x31 or 0xf5 or 0xfb; }

    /// <summary>
    /// Gets a value indicating whether this instance has a DOS memo file.
    /// </summary>
    public bool HasDosMemo { get => (_version & 0x08) != 0; init => _version = (byte)(_version & 0xF7 | (value ? 0x08 : 0x00)); }

    /// <summary>
    /// Gets a value indicating whether this instance has a SQL table.
    /// </summary>
    public int HasSqlTable { get => (_version & 0x70) >> 4; init => _version = (byte)(_version & 0x8F | value << 4 & 0x70); }

    /// <summary>
    /// Gets a value indicating whether this instance has a DBT memo file.
    /// </summary>
    public bool HasDbtMemo { get => (_version & 0x80) != 0; init => _version = (byte)(_version & 0x7F | (value ? 0x80 : 0x00)); }

    /// <summary>
    /// Gets the date of the last update.
    /// </summary>
    public DateOnly LastUpdate { get; private set; } = new(1900, 1, 1);

    /// <summary>
    /// Gets the number of records in the dBase file.
    /// </summary>
    public int RecordCount { get; private set; }

    /// <summary>
    /// Gets the length of the header, in bytes.
    /// </summary>
    public short HeaderLength { get => (short)(32 + 32 * _recordDescriptor.Count + 1); }

    /// <summary>
    /// Gets the length of each record, in bytes.
    /// </summary>
    public short RecordLength { get => _recordDescriptor.RecordSize; }

    /// <summary>
    /// Gets a value indicating whether the data is encrypted.
    /// </summary>
    /// <remarks>
    /// This is only an indication that the content is encrypted.
    /// </remarks>
    public bool IsEncrypted { get; init; }

    /// <summary>
    /// Gets a value indicating whether this instance has an MDX file.
    /// </summary>
    public bool HasMdxFile { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether a transaction is in progress.
    /// </summary>
    public bool InTransaction { get; private set; }

    /// <summary>
    /// Gets the language driver identifier.
    /// </summary>
    /// <remarks>
    /// These values follow the DOS / Windows Code Page values.
    /// </remarks>
    public DbfLanguage Language { get; init; }

    /// <summary>
    /// Gets the encoding for the current <see cref="Language" />.
    /// </summary>
    public Encoding Encoding { get => _encoding ??= Language.GetEncoding(); }

    /// <summary>
    /// Gets the decimal separator for the current <see cref="Language" />.
    /// </summary>
    public char DecimalSeparator { get => _decimalSeparator ??= Language.GetDecimalSeparator(); }

    /// <summary>
    /// Gets the record descriptor.
    /// </summary>
    public ref readonly DbfRecordDescriptor RecordDescriptor { get => ref _recordDescriptor; }

    /// <summary>
    /// Gets the memo file associated with this DBF.
    /// </summary>
    public DbfMemoFile? MemoFile { get; }

    /// <summary>
    /// Gets the <see cref="DbfRecord" /> at the specified index.
    /// </summary>
    /// <param name="index">The record index.</param>
    public DbfRecord this[int index]
    {
        get
        {
            Ensure.InRange(index, 0, RecordCount);

            byte[]? array = null;
            try
            {
                long offset = HeaderLength + RecordLength * index;
                Span<byte> buffer = RecordLength < 256
                    ? stackalloc byte[RecordLength]
                    : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
                _stream.Position = offset;
                _stream.Read(buffer);
                return _recordDescriptor.Read(buffer, this);
            }
            finally
            {
                if (array is not null)
                    ArrayPool<byte>.Shared.Return(array);
            }
        }
        set
        {
            Ensure.InRange(index, 0, RecordCount);

            byte[]? array = null;
            try
            {
                long offset = HeaderLength + RecordLength * index;
                Span<byte> buffer = RecordLength < 256
                    ? stackalloc byte[RecordLength]
                    : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
                _recordDescriptor.Write(value, buffer, this);
                _stream.Position = offset;
                _stream.Write(buffer);
                UpdateLastUpdated();
            }
            finally
            {
                if (array is not null)
                    ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="DbfField" /> with the specified <paramref name="fieldName" />
    /// in the <see cref="DbfRecord" /> at the specified <paramref name="recordIndex" />.
    /// </summary>
    /// <param name="recordIndex">Index of the record.</param>
    /// <param name="fieldName">Name of the field.</param>
    public DbfField this[int recordIndex, string fieldName] { get => this[recordIndex, _recordDescriptor.IndexOf(fieldName)]; }

    /// <summary>
    /// Gets the <see cref="DbfField" /> at the specified <paramref name="fieldIndex" />
    /// in the <see cref="DbfRecord" /> at the specified <paramref name="recordIndex" />.
    /// </summary>
    /// <param name="recordIndex">Index of the record.</param>
    /// <param name="fieldIndex">Index of the field.</param>
    public DbfField this[int recordIndex, int fieldIndex]
    {
        get
        {
            Ensure.InRange(recordIndex, 0, RecordCount);
            Ensure.InRange(fieldIndex, 0, _recordDescriptor.Count);

            byte[]? array = null;
            try
            {
                long offset = HeaderLength + RecordLength * recordIndex;
                Span<byte> buffer = RecordLength < 256
                    ? stackalloc byte[RecordLength]
                    : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
                _stream.Position = offset;
                _stream.Read(buffer);
                return _recordDescriptor.Read(buffer, fieldIndex, this);
            }
            finally
            {
                if (array is not null)
                    ArrayPool<byte>.Shared.Return(array);
            }
        }
        set
        {
            Ensure.InRange(recordIndex, 0, RecordCount);
            Ensure.InRange(fieldIndex, 0, _recordDescriptor.Count);

            byte[]? array = null;
            try
            {
                long offset = HeaderLength + RecordLength * recordIndex;
                Span<byte> buffer = RecordLength < 256
                    ? stackalloc byte[RecordLength]
                    : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
                _recordDescriptor.Write(in value, buffer, fieldIndex, this);
                _stream.Position = offset;
                _stream.Write(buffer);
                UpdateLastUpdated();
            }
            finally
            {
                if (array is not null)
                    ArrayPool<byte>.Shared.Return(array);
            }
        }
    }

    /// <summary>
    /// Copies all data to another file and then returns a new instance of <see cref="Dbf" />
    /// pointing to the newly created file.
    /// </summary>
    /// <param name="fileName">The name of the new file to create.</param>
    public Dbf Clone(string fileName)
    {
        using (var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite))
            _stream.CopyTo(stream);
        return new Dbf(fileName);
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        _stream.Dispose();
        MemoFile?.Dispose();
    }

    private void UpdateLastUpdated()
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (LastUpdate != now)
        {
            LastUpdate = now;
            Span<byte> buffer = stackalloc byte[3]
            {
                (byte)(LastUpdate.Year - 1900),
                (byte)LastUpdate.Month,
                (byte)LastUpdate.Day
            };
            _stream.Position = 1;
            _stream.Write(buffer);
        }
    }

    /// <summary>
    /// Adds the specified record to the file.
    /// </summary>
    /// <param name="record">The record to add.</param>
    /// <exception cref="ArgumentNullException">record</exception>
    public void Add(in DbfRecord record)
    {
        Ensure.NotNull(record);

        byte[]? array = null;
        try
        {
            Span<byte> buffer = RecordLength < 256
                ? stackalloc byte[RecordLength]
                : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
            _stream.Position = HeaderLength + RecordLength * RecordCount;
            _recordDescriptor.Write(record, buffer, this);
            _stream.Write(buffer);
            _stream.WriteByte(EofByte); // EOF.
            UpdateLastUpdated();
            ++RecordCount;
        }
        finally
        {
            if (array is not null)
                ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Adds the specified records to the file.
    /// </summary>
    /// <param name="records">The records to add.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddRange(IEnumerable<DbfRecord> records)
    {
        byte[]? array = null;
        var count = 0;
        try
        {
            Span<byte> buffer = RecordLength < 256
                ? stackalloc byte[RecordLength]
                : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
            _stream.Position = HeaderLength + RecordLength * RecordCount;
            foreach (var record in records)
            {
                _recordDescriptor.Write(record, buffer, this);
                _stream.Write(buffer);
                ++count;
            }
            _stream.WriteByte(EofByte); // EOF.
            UpdateLastUpdated();
            RecordCount += count;
        }
        finally
        {
            if (array is not null)
                ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Removes all records from the file.
    /// </summary>
    public void Clear()
    {
        _stream.SetLength(HeaderLength);
        _stream.Position = HeaderLength;
        _stream.WriteByte(EofByte); // EOF.
        RecordCount = 0;
        UpdateLastUpdated();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<DbfRecord> GetEnumerator()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(RecordLength);
        for (int i = 0; i < RecordCount; ++i)
        {
            long offset = HeaderLength + RecordLength * i;
            _stream.Position = offset;
            _stream.Read(buffer.AsSpan(0, RecordLength));
            yield return _recordDescriptor.Read(buffer.AsSpan(0, RecordLength), this);
        }
        ArrayPool<byte>.Shared.Return(buffer);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines the index of a specific record.
    /// </summary>
    /// <param name="record">The record to locate.</param>
    /// <returns>
    /// The index of <paramref name="record" /> if found; otherwise, -1.
    /// </returns>
    public int IndexOf(in DbfRecord record)
    {
        int index = 0;
        foreach (var elem in this)
        {
            if (elem == record)
                return index;
            ++index;
        }
        return -1;
    }

    /// <summary>
    /// Inserts a record at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="record" /> should be inserted.</param>
    /// <param name="record">The record to insert.</param>
    public void Insert(int index, in DbfRecord record)
    {
        Ensure.InRange(index, 0, RecordCount);
        Ensure.NotNull(record);

        byte[]? array = null;
        try
        {
            Span<byte> buffer = RecordLength < 256
                ? stackalloc byte[RecordLength]
                : (array = ArrayPool<byte>.Shared.Rent(RecordLength)).AsSpan(0, RecordLength);
            var offset = HeaderLength + RecordLength * index;
            _stream.InsertRange(offset, buffer);
            UpdateLastUpdated();
            ++RecordCount;
        }
        finally
        {
            if (array is not null)
                ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Determines whether this file contains the record.
    /// </summary>
    /// <param name="record">The record to locate.</param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="record" /> is found; otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(in DbfRecord record) => IndexOf(record) >= 0;

    /// <summary>
    /// Deletes the first occurrence of a specific record.
    /// </summary>
    /// <param name="record">The record to delete.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="record" /> was successfully deleted in the file; otherwise, <see langword="false" />.
    /// This method also returns <see langword="false" /> if <paramref name="record" /> is not found.
    /// </returns>
    /// <remarks>
    /// Note that this sets record state to deleted but does not actually remove it.
    /// </remarks>
    public bool Delete(ref DbfRecord record)
    {
        var index = IndexOf(record);
        if (index >= 0)
        {
            DeleteAt(index);
            record = record with { RecordStatus = DbfRecord.Status.Deleted };
        }
        return index >= 0;
    }

    /// <summary>
    /// Deletes the record at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the record to delete.</param>
    /// <remarks>
    /// Note that this sets record state to deleted but does not actually remove it.
    /// </remarks>
    public void DeleteAt(int index) => DeleteRange(index, 1);

    /// <summary>
    /// Deletes a range of records in the file.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of records to delete.</param>
    /// <param name="count">The number of records to delete.</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// <remarks>
    /// Note that this sets record state to deleted but does not actually remove it.
    /// </remarks>
    public void DeleteRange(int index, int count) => StatusRange(index, count, DbfRecord.Status.Deleted);

    /// <summary>
    /// Restores the first occurrence of a specific record.
    /// </summary>
    /// <param name="record">The record to restore.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="record" /> was successfully restored in the file; otherwise, <see langword="false" />.
    /// This method also returns <see langword="false" /> if <paramref name="record" /> is not found.
    /// </returns>
    public bool Restore(ref DbfRecord record)
    {
        var index = IndexOf(record);
        if (index >= 0)
        {
            RestoreAt(index);
            record = record with { RecordStatus = DbfRecord.Status.Valid };
        }
        return index >= 0;
    }

    /// <summary>
    /// Restores the record at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the record to restore.</param>
    public void RestoreAt(int index) => RestoreRange(index, 1);

    /// <summary>
    /// Restores a range of records in the file.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of records to restore.</param>
    /// <param name="count">The number of records to restore.</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void RestoreRange(int index, int count) => StatusRange(index, count, DbfRecord.Status.Valid);

    private void StatusRange(int index, int count, DbfRecord.Status status)
    {
        Ensure.GreaterThanOrEqualTo(index, 0);
        Ensure.LessThan(index + count, RecordCount);

        _stream.Position = HeaderLength + RecordLength * index;
        for (int i = 0; i < count; ++i, _stream.Position += RecordLength)
            _stream.WriteByte((byte)status);
        UpdateLastUpdated();
    }

    /// <summary>
    /// Removes the first occurrence of a specific record.
    /// </summary>
    /// <param name="record">The record to remove.</param>
    /// <returns>
    ///   <see langword="true" /> if <paramref name="record" /> was successfully removed from the file; otherwise, <see langword="false" />.
    ///   This method also returns <see langword="false" /> if <paramref name="record" /> is not found.
    /// </returns>
    public bool Remove(in DbfRecord record)
    {
        var index = IndexOf(record);
        if (index >= 0)
            RemoveAt(index);
        return index >= 0;
    }

    /// <summary>
    /// Removes the record at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the record to remove.</param>
    public void RemoveAt(int index) => RemoveRange(index, 1);

    /// <summary>
    /// Removes a range of records from the file.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range of records to remove.</param>
    /// <param name="count">The number of records to remove.</param>
    public void RemoveRange(int index, int count)
    {
        Ensure.GreaterThanOrEqualTo(index, 0);
        Ensure.LessThanOrEqualTo(index + count, RecordCount);

        _stream.RemoveRange(HeaderLength + RecordLength * index, RecordLength * count);
        UpdateLastUpdated();
        RecordCount -= count;
    }

    /// <summary>
    /// Removes all deleted records.
    /// </summary>
    /// <returns>
    /// The actual number of records removed.
    /// </returns>
    public int RemoveDeleted()
    {
        int removed = 0;
        int index = -1, count = 0;
        for (int i = RecordCount - 1; i >= 0; --i)
        {
            _stream.Position = HeaderLength + RecordLength * i;
            // If deleted set index and increase count.
            if ((DbfRecord.Status)_stream.ReadByte() == DbfRecord.Status.Deleted)
            {
                index = i;
                ++count;
            }
            else if (index != -1) // Delete previous range?
            {
                RemoveRange(index, count);
                removed += count;
                index = -1;
                count = 0;
            }
        }
        return removed;
    }
}

file static class SpanExtensions
{
    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
    {
        for (int i = 0; i < span.Length; ++i)
            if (predicate(span[i]))
                return true;
        return false;
    }
}
