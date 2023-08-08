using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lionware.dBase;

/// <summary>
/// Represents a record of a <see cref="Dbf" />.
/// </summary>
/// <remarks>
/// The record if defined by a <see cref="DbfRecordDescriptor" />.
/// </remarks>
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public sealed record class DbfRecord
{
    // Encodes the byte that describes the status of a record.
    internal enum Status : byte { Valid = 0x20, Deleted = 0x2A }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly DbfField[] _fields;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Status _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord" /> class.
    /// </summary>
    /// <param name="fields">The record fields.</param>
    /// <exception cref="ArgumentNullException">fields</exception>
    public DbfRecord(params DbfField[] fields) : this(Status.Valid, fields) { }

    internal DbfRecord(Status status, params DbfField[] fields)
    {
        Ensure.NotNull(fields);

        _status = status;
        _fields = fields;
    }

    /// <summary>
    /// Returns an empty <see cref="DbfRecord"/>.
    /// </summary>
    public static readonly DbfRecord Empty = new(Array.Empty<DbfField>());

    /// <summary>
    /// Gets the number of <see cref="DbfField" /> elements.
    /// </summary>
    internal Status RecordStatus { get => _status; set => _status = value; }

    /// <summary>
    /// Gets the number of <see cref="DbfField" /> elements.
    /// </summary>
    public int Count => _fields.Length;

    /// <summary>
    /// Gets the <see cref="DbfField"/> at the specified index.
    /// </summary>
    public ref readonly DbfField this[int index] { get => ref _fields[index]; }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new(_fields);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfRecord? other)
    {
        if (other is null)
            return false;
        return _status == other._status
            && _fields.SequenceEqual(other._fields);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_status);
        foreach (var field in _fields)
            hash.Add(field);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Enumerates the elements of a <see cref="Span{T}" /> of <see cref="DbfField" />.
    /// </summary>
    public struct Enumerator
    {
        /// <summary>
        /// The span being enumerated.
        /// </summary>
        private readonly DbfField[] _span;
        /// <summary>
        /// The next index to yield.
        /// </summary>
        private int _index;

        /// <summary>
        /// Initialize the enumerator.
        /// </summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(DbfField[] span)
        {
            _span = span;
            _index = -1;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the span.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public readonly ref readonly DbfField Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    private string GetDebuggerDisplay() => $"{nameof(Count)} = {Count}";
}
