using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Lionware;
public sealed class SemanticVersion_should
{
    private static readonly string[] ValidVersions = new string[]
    {
        "0.0.4",
        "1.2.3",
        "10.20.30",
        "1.1.2-prerelease+meta",
        "1.1.2+meta",
        "1.1.2+meta-valid",
        "1.0.0-alpha",
        "1.0.0-beta",
        "1.0.0-alpha.beta",
        "1.0.0-alpha.beta.1",
        "1.0.0-alpha.1",
        "1.0.0-alpha0.valid",
        "1.0.0-alpha.0valid",
        "1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay",
        "1.0.0-rc.1+build.1",
        "2.0.0-rc.1+build.123",
        "1.2.3-beta",
        "10.2.3-DEV-SNAPSHOT",
        "1.2.3-SNAPSHOT-123",
        "1.0.0",
        "2.0.0",
        "1.1.7",
        "2.0.0+build.1848",
        "2.0.1-alpha.1227",
        "1.0.0-alpha+beta",
        "1.2.3----RC-SNAPSHOT.12.9.1--.12+788",
        "1.2.3----R-S.12.9.1--.12+meta",
        "1.2.3----RC-SNAPSHOT.12.9.1--.12",
        "1.0.0+0.build.1-rc.10000aaa-kk-0.1",
        "2147483647.2147483647.2147483647",
        "1.0.0-0A.is.legal",
    };
    private static readonly string[] InvalidVersions = new string[]
    {
        "",
        "1",
        "1.2",
        "1.2.3-0123",
        "1.2.3.4",
        "1.2.3-0123.0123",
        "1.1.2+.123",
        "+invalid",
        "-invalid",
        "-invalid+invalid",
        "-invalid.01",
        "alpha",
        "alpha.beta",
        "alpha.beta.1",
        "alpha.1",
        "alpha+beta",
        "alpha_beta",
        "alpha.",
        "alpha..",
        "beta",
        "1.0.0-alpha_beta",
        "-alpha.",
        "1.0.0-alpha..",
        "1.0.0-alpha..1",
        "1.0.0-alpha...1",
        "1.0.0-alpha....1",
        "1.0.0-alpha.....1",
        "1.0.0-alpha......1",
        "1.0.0-alpha.......1",
        "01.1.1",
        "1.01.1",
        "1.1.01",
        "1.2.3.DEV",
        "1.2-SNAPSHOT",
        "1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788",
        "1.2-RC-SNAPSHOT",
        "-1.0.3-gamma+b7718",
        "+justmeta",
        "9.8.7+meta+meta",
        "9.8.7-whatever+meta+meta",
        "99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12",
    };

    public static IEnumerable<object[]> GetValidVersionsData() => ValidVersions.Select(s => new object[] { s });
    public static IEnumerable<object[]> GetInvalidVersionsData() => InvalidVersions.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(GetValidVersionsData))]
    public void Construct_valid_version(string version) =>
        Assert.Null(Record.Exception(() => new SemanticVersion(version)));

    [Theory]
    [MemberData(nameof(GetInvalidVersionsData))]
    public void Throw_on_invalid_version(string version) =>
        Assert.Throws<ArgumentException>(() => new SemanticVersion(version));

    [Fact]
    public void Throw_on_null() =>
        Assert.Throws<ArgumentNullException>(() => new SemanticVersion(null!));

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Throw_on_negative_numbers(int major, int minor, int patch) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticVersion(major, minor, patch));

    [Theory]
    [MemberData(nameof(GetValidVersionsData))]
    public void Parse_valid_string(string version) =>
        Assert.Null(Record.Exception(() => new SemanticVersion(version)));

    [Theory]
    [MemberData(nameof(GetInvalidVersionsData))]
    public void Throw_when_parsing_invalid_version(string version) =>
        Assert.Throws<FormatException>(() => SemanticVersion.Parse(version));

    [Fact]
    public void Throw_when_parsing_null() =>
        Assert.Throws<ArgumentNullException>(() => SemanticVersion.Parse(null!));

    [Theory]
    [MemberData(nameof(GetValidVersionsData))]
    public void Return_true_when_parsing_valid_string(string version) => Assert.True(SemanticVersion.TryParse(version, out _));

    [Theory]
    [InlineData(null)]
    [MemberData(nameof(GetInvalidVersionsData))]
    public void Return_false_when_parsing_invalid_string(string version) => Assert.False(SemanticVersion.TryParse(version, out _));

    [Theory]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3-preview.4", "1.2.3-preview.4")]
    [InlineData("1.2.3-preview.4", "1.2.3-preview.4+sum-data")]
    public void Return_true_when_versions_are_equal(string v1, string v2)
    {
        var a = new SemanticVersion(v1);
        var b = new SemanticVersion(v2);
        Assert.True(a.Equals(b));
    }

    [Theory]
    [InlineData("1.2.3", "1.2.4")]
    [InlineData("1.2.3-preview.4", "1.2.3")]
    [InlineData("1.2.3-preview.4", "1.2.3-preview.5")]
    public void Return_false_when_versions_are_not_equal(string v1, string v2)
    {
        var a = new SemanticVersion(v1);
        var b = new SemanticVersion(v2);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Compare_versions_correctly()
    {
        var ascStr = new string[] { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0" };

        var descStr = new string[ascStr.Length];
        ascStr.CopyTo(descStr.AsSpan());
        Array.Reverse(descStr);

        var versions = ascStr.Select(static v => new SemanticVersion(v)).OrderBy(_ => Random.Shared.Next()).ToList();

        var asc = versions.Order().Select(v => v.ToString()).ToArray();
        var desc = versions.OrderDescending().Select(v => v.ToString()).ToArray();

        Assert.Equal(ascStr, asc);
        Assert.Equal(descStr, desc);
    }

    [Theory]
    [MemberData(nameof(GetValidVersionsData))]
    public void Roundtrip_json([StringSyntax(StringSyntaxAttribute.Json)] string json)
    {
        json = '"' + json + '"';
        var version = JsonSerializer.Deserialize<SemanticVersion>(json);
        var @string = JsonSerializer.Serialize(version);
        Assert.Equal(json, @string);
    }
}
