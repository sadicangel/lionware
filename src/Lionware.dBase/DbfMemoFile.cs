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

    internal DbfMemoFile(Stream stream, ushort blockSize = DefaultBlockSize)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        BlockSize = blockSize;
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
    /// Appends a new memo at the end of the file and returns it's index.
    /// </summary>
    /// <param name="memo">The memo to be appended.</param>
    /// <returns>The index of the appended memo.</returns>
    public abstract int Append(string memo);

    internal static bool TryOpen(Dbf dbf, [MaybeNullWhen(false)] out DbfMemoFile memoFile)
    {
        memoFile = null;
        var fileName = Path.ChangeExtension(dbf.FileName, ".dbt");

        if (!File.Exists(fileName))
            return false;

        memoFile = dbf.Version switch
        {
            0x83 => new DbfMemoFile03(new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite)),
            _ => throw new NotSupportedException($"Memo file for {dbf.VersionDescription}")
        };

        return true;
    }

    internal static DbfMemoFile Create(Dbf dbf)
    {
        var fileName = Path.ChangeExtension(dbf.FileName, ".dbt");
        return dbf.Version switch
        {
            0x83 => new DbfMemoFile03(CreateStream(fileName)),
            _ => throw new NotSupportedException($"Memo file for {dbf.VersionDescription}")
        };

        static Stream CreateStream(string fileName)
        {
            var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
            stream.SetLength(DefaultBlockSize);
            return stream;
        }
    }
}
