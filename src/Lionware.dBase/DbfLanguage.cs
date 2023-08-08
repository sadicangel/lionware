namespace Lionware.dBase;

/// <summary>
/// Database Language Driver ID.
/// </summary>
public enum DbfLanguage : byte
{
    ///<summary>OEM</summary>
    OEM = 0,
    ///<summary>US MSDOS</summary>
    Codepage_437_US_MSDOS = 0x1,
    ///<summary>International MSDOS</summary>
    Codepage_850_International_MSDOS = 0x2,
    ///<summary>Windows ANSI</summary>
    Codepage_1252_Windows_ANSI = 0x3,
    ///<summary>ANSI</summary>
    ANSI = 0x57,
    ///<summary>Greek MSDOS</summary>
    Codepage_737_Greek_MSDOS = 0x6a,
    ///<summary>EasternEuropean MSDOS</summary>
    Codepage_852_EasternEuropean_MSDOS = 0x64,
    ///<summary>Turkish MSDOS</summary>
    Codepage_857_Turkish_MSDOS = 0x6b,
    ///<summary>Icelandic MSDOS</summary>
    Codepage_861_Icelandic_MSDOS = 0x67,
    ///<summary>Nordic MSDOS</summary>
    Codepage_865_Nordic_MSDOS = 0x66,
    ///<summary>Russian MSDOS</summary>
    Codepage_866_Russian_MSDOS = 0x65,
    ///<summary>Chinese Windows</summary>
    Codepage_950_Chinese_Windows = 0x78,
    ///<summary>Chinese Windows</summary>
    Codepage_936_Chinese_Windows = 0x7a,
    ///<summary>Japanese Windows</summary>
    Codepage_932_Japanese_Windows = 0x7b,
    ///<summary>Hebrew Windows</summary>
    Codepage_1255_Hebrew_Windows = 0x7d,
    ///<summary>Arabic Windows</summary>
    Codepage_1256_Arabic_Windows = 0x7e,
    ///<summary>Eastern European Windows</summary>
    Codepage_1250_Eastern_European_Windows = 0xc8,
    ///<summary>Russian Windows</summary>
    Codepage_1251_Russian_Windows = 0xc9,
    ///<summary>Turkish Windows</summary>
    Codepage_1254_Turkish_Windows = 0xca,
    ///<summary>Greek Windows</summary>
    Codepage_1253_Greek_Windows = 0xcb
}
