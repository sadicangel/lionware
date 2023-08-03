namespace Lionware;
public sealed class EnsureTests
{
    [Fact]
    public void Ensure_NotNull_ThrowsOnNullReference() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNull((object?)null));

    [Fact]
    public void Ensure_NotNull_DoesNotThrowOnNonNullReference() => Assert.Null(Record.Exception(() => Ensure.NotNull(new object())));

    [Fact]
    public unsafe void Ensure_NotNull_ThrowsOnNullPointer() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNull((void*)null));

    [Fact]
    public unsafe void Ensure_NotNull_DoesNotThrowOnNonNullPointer() => Assert.Null(Record.Exception(() => Ensure.NotNull((void*)(nint)20)));

    [Fact]
    public void Ensure_NotNullOrEmpty_ThrowsOnNullString() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNullOrEmpty(null));

    [Fact]
    public void Ensure_NotNullOrEmpty_ThrowsOnEmptyString() => Assert.Throws<ArgumentException>(() => Ensure.NotNullOrEmpty(String.Empty));

    [Fact]
    public void Ensure_NotNullOrEmpty_DoesNotThrowOnNonNullNonEmptyString() => Assert.Null(Record.Exception(() => Ensure.NotNullOrEmpty("string")));

    private static void AssertResult<T>(bool shouldThrow, Action testCode)
    {
        var exception = Record.Exception(testCode);
        if (shouldThrow)
            Assert.IsType<T>(exception);
        else
            Assert.Null(exception);
    }

    [Theory]
    [InlineData(false, 0, -1)]
    [InlineData(true, 0, 0)]
    [InlineData(true, 0, 1)]
    public void Ensure_GreaterThan_AssertsCorrectly(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.GreaterThan(argument, comparand));

    [Theory]
    [InlineData(false, 0, -1)]
    [InlineData(false, 0, 0)]
    [InlineData(true, 0, 1)]
    public void Ensure_GreaterThanOrEqualTo_AssertsCorrectly(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.GreaterThanOrEqualTo(argument, comparand));

    [Theory]
    [InlineData(true, 0, -1)]
    [InlineData(true, 0, 0)]
    [InlineData(false, 0, 1)]
    public void Ensure_LessThan_AssertsCorrectly(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.LessThan(argument, comparand));

    [Theory]
    [InlineData(true, 0, -1)]
    [InlineData(false, 0, 0)]
    [InlineData(false, 0, 1)]
    public void Ensure_LessThanOrEqualTo_AssertsCorrectly(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.LessThanOrEqualTo(argument, comparand));
}
