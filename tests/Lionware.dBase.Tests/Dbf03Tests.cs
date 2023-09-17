namespace Lionware.dBase;
public sealed class Dbf03Tests : DbfTests, IDisposable
{

    public Dbf03Tests() : base("Resources/dbase_03.dbf") { }

    [Fact]
    public void Dbf_Version_IsCorrect()
    {
        Assert.Equal(3, ReadOnlyDbf.Version);
    }

    [Fact]
    public void Dbf_IsFoxPro_IsFalse()
    {
        Assert.False(ReadOnlyDbf.IsFoxPro);
    }

    [Fact]
    public void Dbf_HasDbtMemo_IsFalse()
    {
        Assert.False(ReadOnlyDbf.HasDbtMemo);
    }

    [Fact]
    public void Dbf_HasDosMemo_IsFalse()
    {
        Assert.False(ReadOnlyDbf.HasDosMemo);
    }

    [Fact]
    public void Dbf_RecordCount_IsCorrect()
    {
        Assert.Equal(14, ReadOnlyDbf.RecordCount);
    }

    [Fact]
    public void Dbf_FieldCount_IsCorrect()
    {
        Assert.Equal(31, ReadOnlyDbf.RecordDescriptor.Count);
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

                Assert.Equal(expected, actual);
            }
        }
    }
}