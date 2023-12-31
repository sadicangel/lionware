﻿namespace Lionware.IO;

/// <summary>
/// Extensions for <see cref="Stream" />.
/// </summary>s
public sealed class StreamExtensions_should
{
    [Fact]
    public void Insert_range_at_start()
    {
        using var stream = new MemoryStream();
        stream.Write(stackalloc byte[8] { 2, 3, 4, 5, 6, 7, 8, 9 });

        stream.InsertRange(0, stackalloc byte[2] { 0, 1 });

        var expected = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Insert_range_in_middle()
    {
        using var stream = new MemoryStream();
        stream.Write(stackalloc byte[8] { 0, 1, 2, 3, 6, 7, 8, 9 });

        stream.InsertRange(4, stackalloc byte[2] { 4, 5 });

        var expected = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Insert_range_bigger_than_default_in_middle()
    {
        const int size = 8 * 1234;

        using var stream = new MemoryStream();
        var pattern = Enumerable.Range(0, 128).Select(i => (byte)i).ToArray();
        for (int k = 0; k < size;)
        {
            var bytesToWrite = Math.Min(pattern.Length, size - k);
            stream.Write(pattern.AsSpan(0, bytesToWrite));
            k += bytesToWrite;
        }

        const int offset = 128 * 10;

        stream.InsertRange(offset, Enumerable.Range(128, 128).Select(i => (byte)i).ToArray());

        var result = stream.ToArray();

        var i = 0;
        for (; i < offset; ++i)
            Assert.Equal(i % 128, result[i]);

        for (var j = 0; i < offset + 128; ++i, ++j)
            Assert.Equal(128 + j, result[i]);

        for (; i < result.Length; ++i)
            Assert.Equal(i % 128, result[i]);
    }

    [Fact]
    public void Insert_range_at_end()
    {
        using var stream = new MemoryStream();
        stream.Write(stackalloc byte[8] { 0, 1, 2, 3, 4, 5, 6, 7 });

        stream.InsertRange(8, stackalloc byte[2] { 8, 9 });

        var expected = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Throw_if_insert_range_starts_after_stream_end()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.InsertRange(1, stackalloc byte[0]));
    }

    [Fact]
    public void Remove_range_at_start()
    {
        using var stream = new MemoryStream(Enumerable.Range(0, 10).Select(i => (byte)i).ToArray());

        stream.RemoveRange(..2);

        var expected = new byte[8] { 2, 3, 4, 5, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Remove_range_in_middle()
    {
        using var stream = new MemoryStream(Enumerable.Range(0, 10).Select(i => (byte)i).ToArray());

        stream.RemoveRange(4..6);

        var expected = new byte[8] { 0, 1, 2, 3, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Remove_range_bigger_than_default_in_middle()
    {
        const int size = 8 * 1234;
        const int offset = 128 * 10;

        using var stream = new MemoryStream();
        int i = 0;
        for (; i < offset; ++i)
            stream.WriteByte((byte)(i % 128));

        for (var j = 0; i < offset + 128; ++i, ++j)
            stream.WriteByte((byte)(j + 128));

        for (; i < size; ++i)
            stream.WriteByte((byte)(i % 128));

        stream.RemoveRange(offset, 128);

        var result = stream.ToArray();
        for (i = 0; i < result.Length; i++)
            Assert.Equal(i % 128, result[i]);
    }

    [Fact]
    public void Remove_range_at_end()
    {
        using var stream = new MemoryStream(Enumerable.Range(0, 10).Select(i => (byte)i).ToArray());

        stream.RemoveRange(^2..);

        var expected = new byte[8] { 0, 1, 2, 3, 4, 5, 6, 7 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Ignore_remove_range_if_after_stream_end()
    {
        using var stream = new MemoryStream(Enumerable.Range(0, 10).Select(i => (byte)i).ToArray());

        stream.RemoveRange(10..);

        var expected = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.AsSpan();
        var actual = stream.ToArray().AsSpan();

        Assert.True(expected.SequenceEqual(actual));
    }

    [Fact]
    public void Throw_if_remove_range_start_is_after_stream_end()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.RemoveRange(1, 1));
    }

    [Fact]
    public void Throw_if_remove_range_starts_after_stream_end()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.RemoveRange(1..));
    }

    [Fact]
    public void Throw_if_range_end_is_after_stream_end()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.RemoveRange(0, 5));
    }

    [Fact]
    public void Throw_if_range_ends_after_stream_end()
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.RemoveRange(..5));
    }
}
