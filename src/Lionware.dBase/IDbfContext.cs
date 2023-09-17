using System.Text;

namespace Lionware.dBase;

/// <summary>
/// Holds context data to read and write DBF records or fields.
/// </summary>
internal interface IDbfContext
{
    /// <summary>
    /// Gets the <see cref="System.Text.Encoding"/> to use when reading/writing data.
    /// </summary>
    Encoding Encoding { get; }

    /// <summary>
    /// Gets the decimal separator character to use for decimal numbers.
    /// </summary>
    char DecimalSeparator { get; }
}
