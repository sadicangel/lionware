using System.Diagnostics;

namespace Lionware.dBase;

public sealed class Dbf30_should : IClassFixture<Dbf30_fixture>
{
    private readonly Dbf30_fixture _fixture;

    public Dbf30_should(Dbf30_fixture fixture) => _fixture = fixture;

    [Fact]
    public void Return_0x30_for_Version()
    {
        Assert.Equal(0x30, _fixture.ReadOnlyDbf.Version);
    }

    [Fact]
    public void Return_true_for_IsFoxPro()
    {
        Assert.True(_fixture.ReadOnlyDbf.IsFoxPro);
    }

    //[Fact]
    //public void Return_false_for_HasDbtMemo()
    //{
    //    Assert.False(_fixture.ReadOnlyDbf.HasDbtMemo);
    //}

    //[Fact]
    //public void Return_false_for_HasDosMemo()
    //{
    //    Assert.False(_fixture.ReadOnlyDbf.HasDosMemo);
    //}

    [Fact]
    public void Return_34_for_RecordCount()
    {
        Assert.Equal(34, _fixture.ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Return_145_for_FieldCount()
    {
        Assert.Equal(145, _fixture.ReadOnlyDbf.Schema.FieldCount);
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

                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void Write_values_correctly() => this.WithTempDirectory(dir =>
    {
        var fileName = Path.Combine(dir, "03.dbf");
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
                Debug.Write($"{_fixture.ReadOnlySchema[j].Name}: {expected}");
                var actual = record[j]?.ToString();
                Assert.Equal(expected, actual);
            }
        }
    });
}