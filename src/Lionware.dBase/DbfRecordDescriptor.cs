using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lionware.dBase;

/// <summary>
/// Defines the layout of a <see cref="DbfRecord" />.
/// </summary>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public readonly struct DbfRecordDescriptor : IEquatable<DbfRecordDescriptor>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DbfFieldDescriptor[] _fieldDescriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecordDescriptor" /> class.
    /// </summary>
    /// <param name="fieldDescriptors">The field descriptors.</param>
    /// <exception cref="ArgumentNullException">nameof(fieldDescriptors)</exception>
    /// <exception cref="ArgumentNullException">fieldDescriptors</exception>
    /// <remarks>
    /// Note that the length/decimal of the descriptor are coerced to respect their type constraints.
    /// </remarks>
    public DbfRecordDescriptor(params DbfFieldDescriptor[] fieldDescriptors)
    {
        Ensure.NotNull(fieldDescriptors);
        _fieldDescriptors = fieldDescriptors;
        // Make sure the fields have proper lengths.
        foreach (ref readonly var descriptor in this)
            descriptor.CoerceLength();
    }

    /// <summary>
    /// Gets the DBF field descriptors that make up this instance.
    /// </summary>
    public ReadOnlySpan<DbfFieldDescriptor> FieldDescriptors { get => _fieldDescriptors; }

    /// <summary>
    /// Gets the number of <see cref="DbfFieldDescriptor" /> elements.
    /// </summary>
    public int Count => _fieldDescriptors.Length;

    /// <summary>
    /// Gets the size of the record in bytes.
    /// </summary>
    public short RecordSize
    {
        get
        {
            short length = 1;
            // Make sure the fields have proper lengths.
            foreach (ref readonly var descriptor in this)
                length += descriptor.Length;
            return length;
        }
    }

    /// <summary>
    /// Gets the <see cref="DbfFieldDescriptor"/> at the specified index.
    /// </summary>
    public ref readonly DbfFieldDescriptor this[int index] => ref _fieldDescriptors[index];

    /// <summary>
    /// Gets the <see cref="DbfFieldDescriptor"/> with the specified name.
    /// </summary>
    public ref readonly DbfFieldDescriptor this[string name] => ref _fieldDescriptors[IndexOf(name)];

    /// <summary>
    /// Returns the index of the first field with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the field to search.</param>
    /// <returns>The index of the first field if found; -1 otherwise.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public int IndexOf(string name)
    {
        Ensure.NotNullOrEmpty(name);
        for (int i = 0; i < _fieldDescriptors.Length; ++i)
            if (_fieldDescriptors[i].NameEquals(name))
                return i;

        return -1;
    }

    /// <summary>
    /// Returns the index of the last field with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the field to search.</param>
    /// <returns>The index of the last field if found; -1 otherwise.</returns>
    /// <exception cref="ArgumentNullException">name</exception>
    public int LastIndexOf(string name)
    {
        Ensure.NotNullOrEmpty(name);
        for (int i = _fieldDescriptors.Length - 1; i >= 0; --i)
            if (_fieldDescriptors[i].NameEquals(name))
                return i;

        return -1;
    }

    /// <summary>
    /// Reads a <see cref="DbfRecord"/> from <paramref name="source"/> using the 
    /// properties defined in this instance.
    /// </summary>
    /// <param name="source">The span to read from.</param>
    /// <param name="encoding">The encoding to use when converting bytes to chars.</param>
    /// <param name="decimalSeparator">The separator used for decimal numbers.</param>
    /// <returns></returns>
    public DbfRecord Read(in ReadOnlySpan<byte> source, Encoding encoding, char decimalSeparator)
    {
        var rStatus = (DbfRecord.Status)source[0];

        var buffer = source[1..];
        var rFields = new DbfField[Count];

        for (int i = 0; i < Count; ++i)
        {
            ref readonly var descriptor = ref _fieldDescriptors[i];

            rFields[i] = descriptor.Read(buffer[..descriptor.Length], encoding, decimalSeparator);
            buffer = buffer[descriptor.Length..];
        }

        return new DbfRecord(rStatus, rFields);
    }

    /// <summary>
    /// Writes a <see cref="DbfRecord"/> to the <paramref name="target"/> using the 
    /// properties defined in this instance.
    /// </summary>
    /// <param name="record">The record to write.</param>
    /// <param name="target">The span to write to.</param>
    /// <param name="encoding">The encoding to use when converting bytes to chars.</param>
    /// <param name="decimalSeparator">The separator used for decimal numbers.</param>
    /// <returns></returns>
    public void Write(DbfRecord record, Span<byte> target, Encoding encoding, char decimalSeparator)
    {
        if (Count != record.FieldCount)
            throw new ArgumentException("Invalid record", nameof(record));

        target[0] = (byte)record.RecordStatus;
        target = target[1..];
        for (int i = 0; i < record.FieldCount; ++i)
        {
            ref readonly var descriptor = ref _fieldDescriptors[i];
            ref readonly var field = ref record[i];
            descriptor.Write(in field, target[..descriptor.Length], encoding, decimalSeparator);
            target = target[descriptor.Length..];
        }
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new(_fieldDescriptors);

    /// <summary>Enumerates the elements of a <see cref="Span{T}"/> of <see cref="DbfFieldDescriptor"/>.</summary>
    public ref struct Enumerator
    {
        /// <summary>The span being enumerated.</summary>
        private readonly ReadOnlySpan<DbfFieldDescriptor> _span;
        /// <summary>The next index to yield.</summary>
        private int _index;

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<DbfFieldDescriptor> span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int index = _index + 1;
            if (index < _span.Length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public readonly ref readonly DbfFieldDescriptor Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}; {nameof(RecordSize)} = {RecordSize}";


    /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
    public bool Equals(DbfRecordDescriptor other)
        => Count == other.Count
        && RecordSize == other.RecordSize
        && _fieldDescriptors.SequenceEqual(other._fieldDescriptors);

    /// <inheritdoc cref="object.Equals(object)"/>
    public override bool Equals(object? obj) => obj is DbfRecordDescriptor other && Equals(other);

    /// <inheritdoc cref="object.GetHashCode()"/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Count);
        hash.Add(RecordSize);
        foreach (var descriptor in this)
            hash.Add(descriptor);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public static bool operator ==(DbfRecordDescriptor left, DbfRecordDescriptor right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(DbfRecordDescriptor left, DbfRecordDescriptor right) => !(left == right);
}
