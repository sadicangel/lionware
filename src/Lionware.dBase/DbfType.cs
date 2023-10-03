namespace Lionware.dBase;

/// <summary>
/// Defines the type of a field in a <see cref="DbfRecord" />.
/// </summary>
public enum DbfType : byte
{
    /// <summary>
    /// Any ASCII text (padded with spaces).
    /// <para>In memory representation: <see cref="string"/>.</para>
    /// </summary>
    Character = (byte)'C',
    /// <summary>
    ///   <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c> (right justified, padded with whitespace)
    /// <para>In memory representation: <see cref="double"/>.</para>
    /// </summary>
    Numeric = (byte)'N',
    /// <summary>
    ///   <c>-</c>, <c>.</c>, <c>0</c>-<c>9</c> (right justified, padded with whitespace).
    /// <para>In memory representation: <see cref="double"/>.</para>
    /// </summary>
    /// <remarks>Identical to <see cref="Numeric" />; maintained for compatibility.</remarks>
    Float = (byte)'F',
    /// <summary>
    ///   <see cref="int" />. Optimized for speed.
    /// <para>In memory representation: <see cref="int"/>.</para>
    /// </summary>
    Int32 = (byte)'I',
    /// <summary>
    ///   <see cref="double" />. Optimized for speed.
    /// <para>In memory representation: <see cref="double"/>.</para>
    /// </summary>
    Double = (byte)'O',
    /// <summary>
    /// Automatically incremented <see cref="int" /> values.
    /// <para>In memory representation: <see cref="int"/>.</para>
    /// </summary>
    /// <remarks>
    /// Deleting a row does not change the field values of other rows. Be aware that adding an auto increment field will pack the table.
    /// </remarks>
    AutoIncrement = (byte)'+',
    /// <summary>
    /// Numbers and a character to separate month, day, and year (stored internally as 8 digits in YYYYMMDD format).
    /// <para>In memory representation: <see cref="DateOnly"/>.</para>
    /// </summary>
    Date = (byte)'D',
    /// <summary>
    /// Date/Time stamp, including the Date format plus hours, minutes, and seconds, such as hh:MM:ss.
    /// <para>In memory representation: <see cref="DateTime"/>.</para>
    /// </summary>
    Timestamp = (byte)'T',
    /// <summary>
    ///   <c>Y</c>, <c>y</c>, <c>N</c>, <c>n</c>, <c>T</c>, <c>t</c>, <c>F</c>, <c>f</c>, <c>1</c>, <c>0</c> or <c>?</c> (when uninitialized).
    /// <para>In memory representation: <see cref="bool"/>.</para>
    /// </summary>
    /// <remarks>
    /// In some cases, '<c>?</c>' can be replaced with '<c> </c>' (space character <c>0x20</c>).
    /// </remarks>
    Logical = (byte)'L',
    /// <summary>
    /// Any ASCII text (stored internally as 10 digits representing a .dbt block number, right justified, padded with whitespace).
    /// <para>In memory representation: <see cref="string"/> or <see cref="int"/>.</para>
    /// </summary>
    Memo = (byte)'M',
    /// <summary>
    /// Like <see cref="Memo" /> fields, but not for text processing.
    /// <para>In memory representation: <see cref="string"/> or <see cref="int"/>.</para>
    /// </summary>
    Binary = (byte)'B',
    /// <summary>
    /// OLE Objects in MS Windows versions.
    /// <para>In memory representation: <see cref="string"/> or <see cref="int"/>.</para>
    /// </summary>
    Ole = (byte)'G',
}
