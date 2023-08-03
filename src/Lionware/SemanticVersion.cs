using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Lionware;

/// <summary>
/// An implementation of <see href="https://semver.org">semantic version</see>.
/// </summary>
/// <seealso cref="IEquatable{T}" />
/// <seealso cref="IComparable" />
/// <seealso cref="IComparable{T}" />
[TypeConverter(typeof(SemanticVersionTypeConverter))]
[JsonConverter(typeof(SemanticVersionJsonConverter))]
[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public sealed partial class SemanticVersion : IEquatable<SemanticVersion>, IComparable, IComparable<SemanticVersion>, IParsable<SemanticVersion>, IFormattable
{
    /// <summary>
    /// Gets the major component of this version.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Gets the minor component of this version.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Gets the patch component of this version.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Gets the pre-release label of this version.
    /// </summary>
    public string Prerelease { get; }

    /// <summary>
    /// Gets the build metadata label of this version.
    /// </summary>
    public string Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this version is a pre-release.
    /// </summary>
    public bool IsPrerelease { get => Prerelease.Length > 0; }

    /// <summary>
    /// Gets a value indicating whether this version has build metadata.
    /// </summary>
    public bool HasMetadata { get => Metadata.Length > 0; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <exception cref="ArgumentNullException">version</exception>
    /// <exception cref="ArgumentException">version</exception>
    public SemanticVersion(string version) : this(GetValidMatchOrThrow(version)) { }

    private static Match GetValidMatchOrThrow(string version)
    {
        Ensure.NotNullOrEmpty(version);

        var match = VersionRegex().Match(version);
        if (!match.Success)
            throw new ArgumentException("Argument is not a valid version string", nameof(version));
        return match;
    }

    /// Initializes a new instance with a successful regex match.
    private SemanticVersion(Match match)
    {
        Major = Int32.Parse(match.Groups[nameof(Major)].Value, CultureInfo.InvariantCulture);
        Minor = Int32.Parse(match.Groups[nameof(Minor)].Value, CultureInfo.InvariantCulture);
        Patch = Int32.Parse(match.Groups[nameof(Patch)].Value, CultureInfo.InvariantCulture);
        Prerelease = match.Groups[nameof(Prerelease)].Value;
        Metadata = match.Groups[nameof(Metadata)].Value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticVersion"/> class.
    /// </summary>
    /// <param name="major">The major.</param>
    /// <param name="minor">The minor.</param>
    /// <param name="patch">The patch.</param>
    /// <param name="prerelease">The prerelease.</param>
    /// <param name="buildMetadata">The build metadata.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// major
    /// or
    /// minor
    /// or
    /// patch
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// prerelease
    /// or
    /// buildMetadata
    /// </exception>
    /// <exception cref="ArgumentException">
    /// null - prerelease
    /// or
    /// null - buildMetadata
    /// </exception>
    public SemanticVersion(int major, int minor, int patch, string prerelease = "", string buildMetadata = "")
    {
        Ensure.GreaterThanOrEqualTo(major, 0);
        Ensure.GreaterThanOrEqualTo(minor, 0);
        Ensure.GreaterThanOrEqualTo(patch, 0);
        Ensure.NotNull(prerelease);
        Ensure.NotNull(buildMetadata);
        var prereleaseMatch = PrereleaseRegex().Match(prerelease);
        if (prerelease.Length > 0 && !prereleaseMatch.Success)
            throw new ArgumentException($"prerelease ({prerelease}) is not valid", nameof(prerelease));
        var buildMetadataMatch = BuildMetadataRegex().Match(buildMetadata);
        if (buildMetadata.Length > 0 && !buildMetadataMatch.Success)
            throw new ArgumentException($"buildMetadata ({buildMetadata}) is not valid", nameof(buildMetadata));

        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = GetPrerelease(prereleaseMatch);
        Metadata = buildMetadataMatch.Groups[nameof(Metadata)].Value;

        static string GetPrerelease(Match match)
        {
            var prerelease = match.Groups[nameof(Prerelease)].Value;
            if (!String.IsNullOrEmpty(prerelease) && prerelease[0] == '-')
                return prerelease[1..];
            return prerelease;
        }
    }
    private string GetDebuggerDisplay() => ToString()!;

    // Taken from https://semver.org/
    [GeneratedRegex("^(?<Major>0|[1-9]\\d*)\\.(?<Minor>0|[1-9]\\d*)\\.(?<Patch>0|[1-9]\\d*)(?:-(?<Prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<Metadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();

    [GeneratedRegex("((?<Prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?$", RegexOptions.Compiled)]
    private static partial Regex PrereleaseRegex();

    [GeneratedRegex("((?<Metadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$", RegexOptions.Compiled)]
    private static partial Regex BuildMetadataRegex();

    /// <summary>
    /// Returns the normalized string representation of this <see cref="SemanticVersion" />.
    /// </summary>
    public override string ToString() => ToString("N");

    /// <inheritdoc cref="ToString(string?, IFormatProvider?)" />
    public string ToString(string? format) => ToString(format, null);

    /// <inheritdoc />
    /// <remarks>
    /// Supported formats:
    /// <list type="table">
    /// <item><term>V</term> <description>major.minor.patch</description></item>
    /// <item><term>N</term> <description>major.minor.patch[-prerelease]</description></item>
    /// <item><term>F</term> <description>major.minor.patch[-prerelease][+metadata]</description></item>
    /// <item><term>x</term> <description>major</description></item>
    /// <item><term>y</term> <description>minor</description></item>
    /// <item><term>z</term> <description>patch</description></item>
    /// <item><term>p</term> <description>prerelease</description></item>
    /// <item><term>m</term> <description>metadata</description></item>
    /// </list>
    /// <para>
    /// For example, specifying <c>"x.y"</c> will return major.minor.
    /// </para>
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= "N";

        var builder = new StringBuilder();
        foreach (var c in format)
            Format(builder, c, this);

        return builder.ToString();

        static void Format(StringBuilder builder, char c, SemanticVersion version)
        {
            switch (c)
            {
                case 'N':
                    AppendNormalized(builder, version);
                    return;
                case 'V':
                    AppendVersion(builder, version);
                    return;
                case 'F':
                    AppendFull(builder, version);
                    return;

                case 'x':
                    builder.Append(version.Major);
                    return;
                case 'y':
                    builder.Append(version.Minor);
                    return;
                case 'z':
                    builder.Append(version.Patch);
                    return;
                case 'p':
                    builder.Append(version.Prerelease);
                    return;
                case 'm':
                    builder.Append(version.Metadata);
                    return;

                default:
                    builder.Append(c);
                    return;
            }
        }

        // Appends major.minor.patch only.
        static void AppendVersion(StringBuilder builder, SemanticVersion version)
        {
            builder.Append(version.Major);
            builder.Append('.');
            builder.Append(version.Minor);
            builder.Append('.');
            builder.Append(version.Patch);
        }

        // Appends a normalized version string. This string is unique for each version 'identity' 
        // and does not include leading zeros or metadata.
        static void AppendNormalized(StringBuilder builder, SemanticVersion version)
        {
            AppendVersion(builder, version);

            if (version.IsPrerelease)
            {
                builder.Append('-');
                builder.Append(version.Prerelease);
            }
        }

        // Appends the full version string including metadata. This is primarily for display purposes.
        static void AppendFull(StringBuilder builder, SemanticVersion version)
        {
            AppendNormalized(builder, version);

            if (version.HasMetadata)
            {
                builder.Append('+');
                builder.Append(version.Metadata);
            }
        }
    }

    /// <inheritdoc />
    public bool Equals([AllowNull] SemanticVersion other)
        => other is not null
        && Major == other.Major
        && Minor == other.Minor
        && Patch == other.Patch
        && String.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease);

    /// <inheritdoc />
    public int CompareTo([AllowNull] SemanticVersion other)
    {
        // Precedence MUST be calculated by separating the version into major, minor, patch and
        // prerelease identifiers in that order (Build metadata does not figure into precedence).
        // Precedence is determined by the first difference when comparing each of these identifiers
        // from left to right as follows: Major, minor, and patch versions are always compared numerically.
        // Build metadata MUST be ignored when determining version precedence. Thus two versions that
        // differ only in the build metadata, have the same precedence.
        if (other is null)
            return 1;
        if (Major != other.Major)
            return Major < other.Major ? -1 : 1;
        if (Minor != other.Minor)
            return Minor < other.Minor ? -1 : 1;
        if (Patch != other.Patch)
            return Patch < other.Patch ? -1 : 1;
        // When major, minor, and patch are equal, a prerelease version has lower precedence than a normal version.
        if (String.IsNullOrEmpty(Prerelease))
            return String.IsNullOrEmpty(other.Prerelease) ? 0 : 1;
        if (String.IsNullOrEmpty(other.Prerelease))
            return -1;
        // Precedence for two prerelease versions with the same major, minor, and patch version MUST be determined
        // by comparing each dot separated identifier from left to right until a difference is found.
        var units1 = Prerelease.Split('.');
        var units2 = other.Prerelease.Split('.');
        var length = Math.Min(units1.Length, units2.Length);
        for (int i = 0; i < length; i++)
        {
            var ac = units1[i];
            var bc = units2[i];
            var isNumber1 = Int32.TryParse(ac, out int number1);
            var isNumber2 = Int32.TryParse(bc, out int number2);

            // Identifiers consisting of only digits are compared numerically.
            if (isNumber1 && isNumber2)
            {
                if (number1 != number2)
                    return number1 < number2 ? -1 : 1;
            }
            else
            {
                // Numeric identifiers always have lower precedence than non-numeric identifiers.
                if (isNumber1) return -1;
                if (isNumber2) return 1;
                // Identifiers with letters or hyphens are compared lexically in ASCII sort order.
                if (String.CompareOrdinal(ac, bc) is var result && result != 0)
                    return result;
            }
        }
        // A larger set of prerelease fields has a higher precedence than a smaller set, if all of the preceding identifiers are equal.
        return units1.Length.CompareTo(units2.Length);
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (obj is null)
            return 1;

        if (obj is SemanticVersion other)
            return CompareTo(other);

        throw new ArgumentException($"{nameof(obj)} is not an instance of {nameof(SemanticVersion)}", nameof(obj));
    }

    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)"/>
    public static SemanticVersion Parse(string s) => Parse(s, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public static SemanticVersion Parse(string s, IFormatProvider? provider)
    {
        Ensure.NotNull(s);
        var match = VersionRegex().Match(s);
        if (!match.Success)
            throw new FormatException();
        return new SemanticVersion(match);
    }

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)"/>
    public static bool TryParse([NotNullWhen(true)] string? s, [NotNullWhen(true)] out SemanticVersion? result) => TryParse(s, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SemanticVersion result)
    {
        result = default;
        if (!String.IsNullOrWhiteSpace(s))
        {
            var match = VersionRegex().Match(s);
            if (match.Success)
            {
                result = new SemanticVersion(match);
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
    public static bool operator ==(SemanticVersion left, SemanticVersion right) => left is null ? right is null : left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(SemanticVersion left, SemanticVersion right) => !(left == right);

    /// <inheritdoc />
    public static bool operator <(SemanticVersion left, SemanticVersion right) => left is null ? right is not null : left.CompareTo(right) < 0;

    /// <inheritdoc />
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left is null || left.CompareTo(right) <= 0;

    /// <inheritdoc />
    public static bool operator >(SemanticVersion left, SemanticVersion right) => left is not null && left.CompareTo(right) > 0;

    /// <inheritdoc />
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left is null ? right is null : left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public static implicit operator string?(SemanticVersion? version) => version?.ToString();

    /// <inheritdoc />
    public static explicit operator SemanticVersion?(string? version) => version is not null ? new SemanticVersion(version) : null;
}

file sealed class SemanticVersionJsonConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString() is string version ? new SemanticVersion(version) : default;

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(JsonEncodedText.Encode(value.ToString("F"), JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
}

file sealed class SemanticVersionTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        string @string => new SemanticVersion(@string),
        _ => base.ConvertFrom(context, culture, value),
    };

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        => destinationType == typeof(string) ? value is SemanticVersion version ? version.ToString() : null! : base.ConvertTo(context, culture, value, destinationType);
}