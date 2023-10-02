using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Lionware.dBase;

/// <summary>
/// Represents a dBase memo file.
/// </summary>
/// <seealso cref="IDisposable" />
public abstract class DbfMemoFile : IDisposable
{
    private static readonly byte[] FieldEndMarkerArray = new byte[2] { 0x1A, 0x1A };

    /// <summary>
    /// Default block size.
    /// </summary>
    protected const ushort DefaultBlockSize = 512;

    /// <summary>
    /// Gets the marker that appears at the end of each file.
    /// </summary>
    protected static ReadOnlySpan<byte> FieldEndMarker { get => FieldEndMarkerArray; }

    private bool _disposedValue;

    internal DbfMemoFile(Stream stream, ushort blockSize, bool writeHeader)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        BlockSize = blockSize;
        if (writeHeader)
            WriteHeader();
    }

    /// <summary>
    /// Gets the underlying stream.
    /// </summary>
    protected Stream Stream { get; }

    /// <summary>
    /// Gets the block size in bytes.
    /// </summary>
    /// <remarks>
    /// Fields begin at the start of a block and any remaining space is padded with zeros.
    /// </remarks>
    protected ushort BlockSize { get; }

    /// <summary>
    /// Gets the index of the next block that is available to write.
    /// </summary>
    internal protected int NextAvailableIndex { get; private set; }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                Stream.Dispose();
            }

            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put clean-up code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the memo at the specified index. 
    /// </summary>
    /// <param name="index">The index of the memo to retrieve.</param>
    /// <returns></returns>
    public abstract string this[int index] { get; }

    /// <summary>
    /// Appends a new memo at the end of the file and returns its index.
    /// </summary>
    /// <param name="memo">The memo to be appended.</param>
    /// <returns>The index of the appended memo.</returns>
    public abstract int Append(string memo);

    /// <summary>
    /// Writes the header of the memo file an returns the first free index.
    /// </summary>
    /// <returns>The next free index to be written with memo data.</returns>
    protected abstract int WriteHeader();

    /// <summary>
    /// Updates the next available index.
    /// </summary>
    protected void UpdateBlockSize(ushort blockSize)
    {
        var position = Stream.Position;
        Stream.Position = 0;
        Span<byte> u16 = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(u16, blockSize);
        Stream.Write(u16);
        Stream.Position = position;
    }

    /// <summary>
    /// Updates the next available index.
    /// </summary>
    protected void UpdateNextAvailableIndex(int index)
    {
        var position = Stream.Position;
        Stream.Position = 0;
        Span<byte> u32 = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(u32, (uint)index);
        Stream.Write(u32);
        Stream.Position = position;
        NextAvailableIndex = index;
    }

    internal static bool TryOpen(Dbf dbf, [MaybeNullWhen(false)] out DbfMemoFile memoFile)
    {
        memoFile = null;
        var fileName = Path.ChangeExtension(dbf.FileName, ".dbt");

        if (!File.Exists(fileName))
            return false;

        memoFile = dbf.Version switch
        {
            0x83 => new DbfMemoFileV3(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite), writeHeader: false),
            _ => throw new NotSupportedException($"Memo file for {dbf.VersionDescription}")
        };

        return true;
    }

    internal static DbfMemoFile Create(Dbf dbf)
    {
        var fileName = Path.ChangeExtension(dbf.FileName, ".dbt");
        var memoFile = dbf.Version switch
        {
            0x83 => new DbfMemoFileV3(new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite), writeHeader: true),
            _ => throw new NotSupportedException($"Memo file for {dbf.VersionDescription}")
        };
        return memoFile;
    }
}
