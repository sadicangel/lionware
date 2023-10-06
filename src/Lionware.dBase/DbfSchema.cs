namespace Lionware.dBase;

/// <summary>
/// Defines the layout of records in a <see cref="Dbf" />.
/// </summary>
public sealed class DbfSchema : IEquatable<DbfSchema>
{
    private readonly DbfFieldDescriptor[] _descriptors;
    private readonly DbfFieldReader[] _readers;
    private readonly DbfFieldWriter[] _writers;
    private readonly int[] _offsets;
    private readonly int[] _lengths;
    private readonly Dictionary<string, int> _indices;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbfSchema" /> class.
    /// </summary>
    /// <param name="descriptors">The field descriptors.</param>
    /// <exception cref="ArgumentNullException">nameof(fieldDescriptors)</exception>
    /// <exception cref="ArgumentNullException">fieldDescriptors</exception>
    /// <remarks>
    /// Note that the length/decimal of the descriptor are coerced to respect their type constraints.
    /// </remarks>
    public DbfSchema(params DbfFieldDescriptor[] descriptors)
    {
        _descriptors = descriptors;
        _readers = new DbfFieldReader[descriptors.Length];
        _writers = new DbfFieldWriter[descriptors.Length];
        _offsets = new int[descriptors.Length];
        _lengths = new int[descriptors.Length];
        _indices = new Dictionary<string, int>(_descriptors.Length);
        var recordLength = 1; // Status byte.
        for (int i = 0; i < _descriptors.Length; ++i)
        {
            ref readonly var descriptor = ref _descriptors[i];
            //descriptor.CoerceLength();
            _readers[i] = descriptor.CreateReader();
            _writers[i] = descriptor.CreateWriter();
            _offsets[i] = recordLength;
            _lengths[i] = descriptor.Length;
            _indices[descriptor.Name] = i;
            recordLength += descriptor.Length;
        }
        RecordLength = recordLength;
    }

    /// <summary>
    /// Gets the <see cref="DbfFieldDescriptor"/> at the specified index.
    /// </summary>
    public ReadOnlySpan<DbfFieldDescriptor> Descriptors { get => _descriptors; }

    /// <summary>
    /// Gets the size of the record in bytes.
    /// </summary>
    public int RecordLength { get; }

    /// <summary>
    /// Gets the number of fields in each record.
    /// </summary>
    public int FieldCount { get => _descriptors.Length; }

    /// <summary>
    /// Gets the <see cref="DbfFieldDescriptor"/> at the specified index.
    /// </summary>
    public ref readonly DbfFieldDescriptor this[int index] { get => ref _descriptors[index]; }

    /// <summary>
    /// Gets the <see cref="DbfFieldDescriptor"/> with the specified name.
    /// </summary>
    public ref readonly DbfFieldDescriptor this[string name] { get => ref _descriptors[IndexOf(name)]; }

    /// <summary>
    /// Gets the byte index, from the record start, of the field with the specified name.
    /// </summary>
    /// <param name="name">The name of the field to get the index.</param>
    /// <returns></returns>
    public int IndexOf(string name) => _indices.GetValueOrDefault(name, -1);

    /// <summary>
    /// Gets the byte offset, from the record start, of the field at the specified index.
    /// </summary>
    /// <param name="index">The index of the field to get the offset.</param>
    /// <returns></returns>
    public int OffsetOf(int index) => _offsets[index];

    /// <summary>
    /// Gets the length, in bytes, of the field at the specified index.
    /// </summary>
    /// <param name="index">The index of the field to get the length.</param>
    /// <returns></returns>
    public int LengthOf(int index) => _lengths[index];

    internal object?[] Read(ReadOnlySpan<byte> source, IDbfContext context)
    {
        var record = new object?[_readers.Length];
        for (int i = 0; i < record.Length; ++i)
            record[i] = _readers[i].Invoke(source.Slice(OffsetOf(i), LengthOf(i)), context);
        return record;
    }

    internal object? Read(int index, ReadOnlySpan<byte> source, IDbfContext context)
    {
        return _readers[index].Invoke(source, context);
    }

    internal void Write(object?[] values, Span<byte> target, IDbfContext context)
    {
        Ensure.NotNull(values);
        if (values.Length != _writers.Length)
            throw new ArgumentOutOfRangeException(nameof(values));
        for (int i = 0; i < _writers.Length; ++i)
            _writers[i].Invoke(values[i], target.Slice(OffsetOf(i), LengthOf(i)), context);
    }

    internal void Write(int index, object? value, Span<byte> target, IDbfContext context)
    {
        _writers[index].Invoke(value, target, context);
    }

    /// <inheritdoc/>
    public bool Equals(DbfSchema? other) => other is not null && _descriptors.SequenceEqual(other._descriptors);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as DbfSchema);

    /// <inheritdoc/>
    public override int GetHashCode() => _descriptors.Aggregate(new HashCode(), (hash, desc) => { hash.Add(desc); return hash; }, hash => hash.ToHashCode());
}
