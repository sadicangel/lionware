using System.Collections;

namespace Lionware.dBase;

/// <summary>
/// Describes the status of a <see cref="DbfRecord"/>.
/// </summary>
public enum DbfRecordStatus : byte
{
    /// <summary>
    /// The record is valid.
    /// </summary>
    Valid = 0x20,
    /// <summary>
    /// The record is flagged for removal.
    /// </summary>
    Deleted = 0x2A
}

/// <summary>
/// Represents a record of a <see cref="Dbf" />.
/// </summary>
public sealed class DbfRecord : IReadOnlyList<object?>
{
    private readonly Dbf _dbf;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfRecord" /> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">fields</exception>
    internal DbfRecord(Dbf dbf, int index)
    {
        Ensure.NotNull(dbf);
        Ensure.GreaterThanOrEqualTo(index, 0);

        _dbf = dbf;
        Index = index;
    }

    /// <summary>
    /// Gets the index of the record within the <see cref="Dbf"/>.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the <see cref="DbfRecordStatus"/> of the record.
    /// </summary>
    public DbfRecordStatus Status { get => _dbf.ReadRecordStatus(Index); set => _dbf.WriteRecordStatus(Index, value); }

    /// <inheritdoc/>
    public int Count { get; }

    /// <summary>
    /// Gets or sets the value of the field at the specified index.
    /// </summary>
    public object? this[int index] { get => _dbf[Index, index]; set => _dbf[Index, index] = value; }

    /// <summary>
    /// Gets or sets the value of the field with the specified name.
    /// </summary>
    public object? this[string name] { get => _dbf[Index, name]; set => _dbf[Index, name] = value; }

    public T GetValue<T>(int index) => (T)this[index];
    public T GetValue<T>(string name) => (T)this[name];

    public void SetValue<T>(int index, T? value) => this[index] = value;
    public void SetValue<T>(string name, T? value) => this[name] = value;

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    ///   <see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(DbfRecord other) => Index == other.Index;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public override int GetHashCode() => Index.GetHashCode();

    /// <inheritdoc/>
    public IEnumerator<object?> GetEnumerator()
    {
        foreach (var value in _dbf.ReadRecordValues(Index))
            yield return value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
