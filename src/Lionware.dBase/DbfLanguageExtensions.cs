using System.ComponentModel;
using System.Text;

namespace Lionware.dBase;

/// <summary>
/// Extensions for <see cref="DbfLanguage" />.
/// </summary>
public static class DbfLanguageExtensions
{
    /// <summary>
    /// Gets the decimal separator <see cref="char" /> for the <see cref="DbfLanguage" />.
    /// </summary>
    /// <param name="language">The language <i>codepage</i>.</param>
    /// <returns></returns>
    public static char GetDecimalSeparator(this DbfLanguage language) => language switch
    {
        DbfLanguage.OEM or
        DbfLanguage.ANSI or
        DbfLanguage.Codepage_737_Greek_MSDOS or
        DbfLanguage.Codepage_852_EasternEuropean_MSDOS or
        DbfLanguage.Codepage_1253_Greek_Windows or
        DbfLanguage.Codepage_857_Turkish_MSDOS or
        DbfLanguage.Codepage_861_Icelandic_MSDOS or
        DbfLanguage.Codepage_865_Nordic_MSDOS or
        DbfLanguage.Codepage_866_Russian_MSDOS or
        DbfLanguage.Codepage_1250_Eastern_European_Windows or
        DbfLanguage.Codepage_1254_Turkish_Windows
            => ',',
        DbfLanguage.Codepage_437_US_MSDOS or
        DbfLanguage.Codepage_932_Japanese_Windows or
        DbfLanguage.Codepage_936_Chinese_Windows or
        DbfLanguage.Codepage_950_Chinese_Windows
            => '.',
        DbfLanguage.Codepage_1252_Windows_ANSI or
        DbfLanguage.Codepage_1255_Hebrew_Windows or
        DbfLanguage.Codepage_850_International_MSDOS or
        DbfLanguage.Codepage_1256_Arabic_Windows
            => '.',
        DbfLanguage.Codepage_1251_Russian_Windows
            => ' ',
        _
            => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
    };

    /// <summary>
    /// Gets the <see cref="Encoding"/> associated with the <see cref="DbfLanguage" />.
    /// </summary>
    /// <param name="language">The language <i>codepage</i>.</param>
    /// <returns></returns>
    public static Encoding GetEncoding(this DbfLanguage language) => language switch
    {
        DbfLanguage.Codepage_437_US_MSDOS => Encoding.GetEncoding("IBM437"),
        DbfLanguage.Codepage_737_Greek_MSDOS => Encoding.GetEncoding("ibm737"),
        DbfLanguage.Codepage_850_International_MSDOS => Encoding.GetEncoding("ibm850"),
        DbfLanguage.Codepage_852_EasternEuropean_MSDOS => Encoding.GetEncoding("ibm852"),
        DbfLanguage.Codepage_857_Turkish_MSDOS => Encoding.GetEncoding("ibm857"),
        DbfLanguage.Codepage_861_Icelandic_MSDOS => Encoding.GetEncoding("ibm861"),
        DbfLanguage.Codepage_865_Nordic_MSDOS => Encoding.GetEncoding("IBM865"),
        DbfLanguage.Codepage_866_Russian_MSDOS => Encoding.GetEncoding("cp866"),
        DbfLanguage.Codepage_932_Japanese_Windows => Encoding.GetEncoding("shift_jis"),
        DbfLanguage.Codepage_936_Chinese_Windows => Encoding.GetEncoding("gb2312"),
        DbfLanguage.Codepage_950_Chinese_Windows => Encoding.GetEncoding("big5"),
        DbfLanguage.Codepage_1250_Eastern_European_Windows => Encoding.GetEncoding("windows-1250"),
        DbfLanguage.Codepage_1251_Russian_Windows => Encoding.GetEncoding("windows-1251"),
        DbfLanguage.Codepage_1252_Windows_ANSI => Encoding.GetEncoding("windows-1252"),
        DbfLanguage.Codepage_1253_Greek_Windows => Encoding.GetEncoding("windows-1253"),
        DbfLanguage.Codepage_1254_Turkish_Windows => Encoding.GetEncoding("windows-1254"),
        DbfLanguage.Codepage_1255_Hebrew_Windows => Encoding.GetEncoding("windows-1255"),
        DbfLanguage.Codepage_1256_Arabic_Windows => Encoding.GetEncoding("windows-1256"),
        DbfLanguage.OEM or DbfLanguage.ANSI => Encoding.ASCII,
        _ => throw new InvalidEnumArgumentException(nameof(language), (int)language, typeof(DbfLanguage)),
    };
}
