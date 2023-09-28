﻿using System.Diagnostics;

namespace Lionware.dBase;
public sealed class Dbf03_should : IClassFixture<Dbf03Fixture>
{
    private readonly Dbf03Fixture _fixture;

    public Dbf03_should(Dbf03Fixture fixture) => _fixture = fixture;

    [Fact]
    public void Return_3_for_Version()
    {
        Assert.Equal(3, _fixture.ReadOnlyDbf.Version);
    }

    [Fact]
    public void Return_false_for_IsFoxPro()
    {
        Assert.False(_fixture.ReadOnlyDbf.IsFoxPro);
    }

    [Fact]
    public void Return_false_for_HasDbtMemo()
    {
        Assert.False(_fixture.ReadOnlyDbf.HasDbtMemo);
    }

    [Fact]
    public void Return_false_for_HasDosMemo()
    {
        Assert.False(_fixture.ReadOnlyDbf.HasDosMemo);
    }

    [Fact]
    public void Return_14_for_RecordCount()
    {
        Assert.Equal(14, _fixture.ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Return_31_for_FieldCount()
    {
        Assert.Equal(31, _fixture.ReadOnlyDbf.RecordDescriptor.Count);
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
                Debug.Write($"{_fixture.ReadOnlySchema[j].NameString}: {expected}");
                var actual = record[j].ToString();
                Assert.Equal(expected, actual);
            }
        }
    });
}