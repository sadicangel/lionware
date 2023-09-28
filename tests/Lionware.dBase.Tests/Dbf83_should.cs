using System.Diagnostics;

namespace Lionware.dBase;
public sealed class Dbf83_should : IClassFixture<Dbf83Fixture>
{
    private readonly Dbf83Fixture _fixture;

    public Dbf83_should(Dbf83Fixture fixture) => _fixture = fixture;

    [Fact]
    public void Return_131_for_Version()
    {
        Assert.Equal(0x83, _fixture.ReadOnlyDbf.Version);
    }

    [Fact]
    public void Return_false_for_IsFoxPro()
    {
        Assert.False(_fixture.ReadOnlyDbf.IsFoxPro);
    }

    [Fact]
    public void Return_true_for_HasDbtMemo()
    {
        Assert.True(_fixture.ReadOnlyDbf.HasDbtMemo);
    }

    [Fact]
    public void Return_false_for_HasDosMemo()
    {
        Assert.False(_fixture.ReadOnlyDbf.HasDosMemo);
    }

    [Fact]
    public void Return_67_for_RecordCount()
    {
        Assert.Equal(67, _fixture.ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Return_15_for_FieldCount()
    {
        Assert.Equal(15, _fixture.ReadOnlyDbf.RecordDescriptor.Count);
    }

    [Fact]
    public void Have_expected_record_schema()
    {
        Assert.Equal(_fixture.ReadOnlySchema, _fixture.ReadOnlyDbf.RecordDescriptor);
    }

    [Fact]
    public void Read_values_correctly()
    {
        for (int i = 0; i < _fixture.ReadOnlyDbf.RecordCount; i++)
        {
            var record = _fixture.ReadOnlyDbf[i];
            var values = _fixture.ReadOnlyValues[i];
            for (int j = 0; j < record.FieldCount; j++)
            {
                var actual = record[j].ToString();
                var expected = values[j];

                Debug.WriteLine($"{_fixture.ReadOnlySchema[j].NameString}: {expected}");
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void Write_values_correctly() => this.WithTempDirectory(dir =>
    {
        var fileName = Path.Combine(dir, "83.dbf");
        using var dbf = new Dbf(fileName, _fixture.ReadOnlySchema);
        foreach (var csvRecord in _fixture.ReadOnlyValues)
        {
            var fields = new DbfField[csvRecord.Length];
            for (int i = 0; i < csvRecord.Length; ++i)
                fields[i] = _fixture.ReadOnlySchema[i].ParseField(csvRecord[i]);
            var record = new DbfRecord(fields);
            dbf.Add(in record);
        }

        for (int i = 0; i < dbf.RecordCount; i++)
        {
            var record = dbf[i];
            var values = _fixture.ReadOnlyValues[i];
            for (int j = 0; j < record.FieldCount; j++)
            {
                var expected = values[j];
                Debug.WriteLine($"{_fixture.ReadOnlySchema[j].NameString}: {expected}");
                var actual = record[j].ToString();
                Assert.Equal(expected, actual);
            }
        }
    });
}