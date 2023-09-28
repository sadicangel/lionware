namespace Lionware;
public sealed class Ensure_should
{
    [Fact]
    public void Throw_on_null_reference() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNull((object?)null));

    [Fact]
    public void Not_throw_on_non_null_reference() => Assert.Null(Record.Exception(() => Ensure.NotNull(new object())));

    [Fact]
    public unsafe void Throw_on_null_pointer() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNull((void*)null));

    [Fact]
    public unsafe void Not_throw_on_non_null_pointer() => Assert.Null(Record.Exception(() => Ensure.NotNull((void*)(nint)20)));

    [Fact]
    public void Throw_on_null_string() => Assert.Throws<ArgumentNullException>(() => Ensure.NotNullOrEmpty(null));

    [Fact]
    public void Throw_on_empty_string() => Assert.Throws<ArgumentException>(() => Ensure.NotNullOrEmpty(String.Empty));

    [Fact]
    public void Not_throw_on_non_null_non_empty_string() => Assert.Null(Record.Exception(() => Ensure.NotNullOrEmpty("string")));

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
    public void Assert_GreaterThan(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.GreaterThan(argument, comparand));

    [Theory]
    [InlineData(false, 0, -1)]
    [InlineData(false, 0, 0)]
    [InlineData(true, 0, 1)]
    public void Assert_GreaterThanOrEqualTo(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.GreaterThanOrEqualTo(argument, comparand));

    [Theory]
    [InlineData(true, 0, -1)]
    [InlineData(true, 0, 0)]
    [InlineData(false, 0, 1)]
    public void Assert_LessThan(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.LessThan(argument, comparand));

    [Theory]
    [InlineData(true, 0, -1)]
    [InlineData(false, 0, 0)]
    [InlineData(false, 0, 1)]
    public void Assert_LessThanOrEqualTo(bool throws, int argument, int comparand) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.LessThanOrEqualTo(argument, comparand));

    [Theory]
    [InlineData(false, -1, -1, 1)]
    [InlineData(false, 0, -1, 1)]
    [InlineData(true, 1, -1, 1)]
    [InlineData(true, -2, -1, 1)]
    [InlineData(true, 2, -1, 1)]
    public void Assert_InRange(bool throws, int argument, int lowerBound, int upperBound) =>
        AssertResult<ArgumentOutOfRangeException>(throws, () => Ensure.InRange(argument, lowerBound, upperBound));
}
