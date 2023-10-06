using System.Diagnostics;

namespace Lionware.dBase;

public sealed class Dbf8b_should : IClassFixture<Dbf8b_fixture>
{
    private readonly Dbf8b_fixture _fixture;

    public Dbf8b_should(Dbf8b_fixture fixture) => _fixture = fixture;

    [Fact]
    public void Return_0x8b_for_Version()
    {
        Assert.Equal(0x8b, _fixture.ReadOnlyDbf.Version);
    }

    [Fact]
    public void Return_false_for_IsFoxPro()
    {
        Assert.False(_fixture.ReadOnlyDbf.IsFoxPro);
    }

    //[Fact]
    //public void Return_true_for_HasDbtMemo()
    //{
    //    Assert.True(_fixture.ReadOnlyDbf.HasDbtMemo);
    //}

    //[Fact]
    //public void Return_false_for_HasDosMemo()
    //{
    //    Assert.False(_fixture.ReadOnlyDbf.HasDosMemo);
    //}

    [Fact]
    public void Return_10_for_RecordCount()
    {
        Assert.Equal(10, _fixture.ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Return_6_for_FieldCount()
    {
        Assert.Equal(6, _fixture.ReadOnlyDbf.Schema.FieldCount);
    }

    [Fact]
    public void Have_expected_record_schema()
    {
        Assert.Equal(_fixture.ReadOnlySchema, _fixture.ReadOnlyDbf.Schema);
    }

    [Fact]
    public void Read_values_correctly()
    {
        for (int i = 0; i < _fixture.ReadOnlyDbf.RecordCount; i++)
        {
            var record = _fixture.ReadOnlyDbf[i];
            var values = _fixture.ReadOnlyValues[i];
            for (int j = 0; j < record.Count; j++)
            {
                var actual = record[j]?.ToString();
                var expected = values[j];

                Debug.WriteLine($"{_fixture.ReadOnlySchema[j].Name}: {expected}");
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void Write_values_correctly() => this.WithTempDirectory(dir =>
    {
        var fileName = Path.Combine(dir, "8b.dbf");
        using var dbf = new Dbf(fileName, _fixture.ReadOnlySchema);
        foreach (var csvRecord in _fixture.ReadOnlyValues)
        {
            var fields = new object?[csvRecord.Length];
            for (int i = 0; i < csvRecord.Length; ++i)
                fields[i] = _fixture.ReadOnlySchema[i].ParseField(csvRecord[i]);
            dbf.Add(fields);
        }

        for (int i = 0; i < dbf.RecordCount; i++)
        {
            var record = dbf[i];
            var values = _fixture.ReadOnlyValues[i];
            for (int j = 0; j < record.Count; j++)
            {
                var expected = values[j];
                Debug.WriteLine($"{_fixture.ReadOnlySchema[j].Name}: {expected}");
                var actual = record[j]?.ToString();
                Assert.Equal(expected, actual);
            }
        }
    });
}