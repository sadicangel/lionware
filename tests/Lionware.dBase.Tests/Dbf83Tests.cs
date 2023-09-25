using System.Diagnostics;

namespace Lionware.dBase;
public sealed class Dbf83Tests : DbfTestsBase, IDisposable
{
    public Dbf83Tests() : base("Resources/83.dbf") { }

    [Fact]
    public void Dbf_Version_IsCorrect()
    {
        Assert.Equal(0x83, ReadOnlyDbf.Version);
    }

    [Fact]
    public void Dbf_IsFoxPro_IsCorrect()
    {
        Assert.False(ReadOnlyDbf.IsFoxPro);
    }

    [Fact]
    public void Dbf_HasDbtMemo_IsCorrect()
    {
        Assert.True(ReadOnlyDbf.HasDbtMemo);
    }

    [Fact]
    public void Dbf_HasDosMemo_IsCorrect()
    {
        Assert.False(ReadOnlyDbf.HasDosMemo);
    }

    [Fact]
    public void Dbf_RecordCount_IsCorrect()
    {
        Assert.Equal(67, ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Dbf_FieldCount_IsCorrect()
    {
        Assert.Equal(15, ReadOnlyDbf.RecordDescriptor.Count);
    }

    [Fact]
    public void Dbf_RecordDescriptor_IsValid()
    {
        Assert.Equal(ReadOnlySchema, ReadOnlyDbf.RecordDescriptor);
    }

    [Fact]
    public void Dbf_RecordValues_AreValid()
    {
        for (int i = 0; i < ReadOnlyDbf.RecordCount; i++)
        {
            var record = ReadOnlyDbf[i];
            var values = ReadOnlyValues[i];
            for (int j = 0; j < record.FieldCount; j++)
            {
                var actual = record[j].ToString();
                var expected = values[j];

                Debug.WriteLine($"{ReadOnlySchema[j].NameString}: {expected}");
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void Dbf_Write_RecordValues_AreValid() => WithTempDirectory(dir =>
    {
        var fileName = Path.Combine(dir, "83.dbf");
        using var dbf = new Dbf(fileName, ReadOnlySchema);
        foreach (var csvRecord in ReadOnlyValues)
        {
            var fields = new DbfField[csvRecord.Length];
            for (int i = 0; i < csvRecord.Length; ++i)
                fields[i] = ReadOnlySchema[i].ParseField(csvRecord[i]);
            var record = new DbfRecord(fields);
            dbf.Add(in record);
        }

        for (int i = 0; i < dbf.RecordCount; i++)
        {
            var record = dbf[i];
            var values = ReadOnlyValues[i];
            for (int j = 0; j < record.FieldCount; j++)
            {
                var expected = values[j];
                Debug.WriteLine($"{ReadOnlySchema[j].NameString}: {expected}");
                var actual = record[j].ToString();
                Assert.Equal(expected, actual);
            }
        }
    });
}