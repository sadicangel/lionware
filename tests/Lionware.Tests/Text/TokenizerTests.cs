using System.Buffers;
using System.Globalization;

namespace Lionware.Text;
public sealed class TokenizerTests
{
    public sealed record class TokenInfo(TokenType Type, string Text);
    private class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;

            return segment;
        }
    }

    private static readonly IReadOnlyList<TokenInfo> Tokens = new List<TokenInfo>
    {
        new TokenInfo(TokenType.EOF, ""),
        new TokenInfo(TokenType.LineBreak, "\n"),
        new TokenInfo(TokenType.LineBreak, "\r\n"),
        new TokenInfo(TokenType.Whitespace, " "),
        new TokenInfo(TokenType.Whitespace, "\t"),
        new TokenInfo(TokenType.QuotedString, "\"quoted\""),
        new TokenInfo(TokenType.QuotedString, "\"123\""),
        new TokenInfo(TokenType.String, "string"),
        new TokenInfo(TokenType.String, "enum1"),
        new TokenInfo(TokenType.String, "enum2"),
        new TokenInfo(TokenType.Number, "123"),
        new TokenInfo(TokenType.Number, "-123"),
        new TokenInfo(TokenType.Number, "1.2"),
        new TokenInfo(TokenType.Number, "1.2e2"),
        new TokenInfo(TokenType.Number, "1.2e+2"),
        new TokenInfo(TokenType.Number, "1.2e-2"),
        new TokenInfo(TokenType.Symbol, "+"),
        new TokenInfo(TokenType.Symbol, "-"),
    };

    public static IEnumerable<object[]> GetTokensData() => Tokens.Select(t => new object[] { t });

    private static List<TokenInfo> ParseTokensFromSpan(string text)
    {
        var tokenizer = new Tokenizer((ReadOnlySpan<char>)text);
        var list = new List<TokenInfo>();
        while (tokenizer.Read())
            list.Add(new TokenInfo(tokenizer.TokenType, tokenizer.IsValueSpan ? tokenizer.ValueSpan.ToString() : tokenizer.ValueSequence.ToString()));
        return list;
    }

    private static List<TokenInfo> ParseTokensFromSequence(string text)
    {
        var half = text.Length / 2;
        var start = new MemorySegment<char>(text.AsMemory(..half));
        var end = start.Append(text.AsMemory(half..));
        var tokenizer = new Tokenizer(new ReadOnlySequence<char>(start, 0, end, end.Memory.Length));
        var list = new List<TokenInfo>();
        while (tokenizer.Read())
            list.Add(new TokenInfo(tokenizer.TokenType, tokenizer.IsValueSpan ? tokenizer.ValueSpan.ToString() : tokenizer.ValueSequence.ToString()));
        return list;
    }

    [Theory]
    [MemberData(nameof(GetTokensData))]
    public void Tokenizer_Tokenizes_TokenSpan(TokenInfo tokenInfo)
    {
        var token = ParseTokensFromSpan(tokenInfo.Text).First();

        Assert.Equal(tokenInfo.Type, token.Type);
        Assert.Equal(tokenInfo.Text, token.Text);
    }

    [Theory]
    [MemberData(nameof(GetTokensData))]
    public void Tokenizer_Tokenizes_TokenSequence(TokenInfo tokenInfo)
    {
        var token = ParseTokensFromSequence(tokenInfo.Text).First();

        Assert.Equal(tokenInfo.Type, token.Type);
        Assert.Equal(tokenInfo.Text, token.Text);
    }

    public static IEnumerable<object[]> GetTokenPairsData()
    {
        var validTokens = Tokens.Where(t => t.Type is not TokenType.EOF and not TokenType.LineBreak and not TokenType.Whitespace).ToList();

        return validTokens
            .Zip(validTokens)
            .Select(pair => new object[] { pair.First, pair.Second, RequiresSeparator(pair.First, pair.Second) });

        static bool RequiresSeparator(TokenInfo left, TokenInfo right)
        {
            if (left.Type is TokenType.String && right.Type is TokenType.String or TokenType.Number)
                return true;

            if (left.Type is TokenType.Number && right.Type is TokenType.Number)
                return true;

            return false;
        }
    }

    [Theory]
    [MemberData(nameof(GetTokenPairsData))]
    public void Tokenizer_Tokenizes_PairsSpan(TokenInfo left, TokenInfo right, bool requiresSeparator)
    {
        var tokens = ParseTokensFromSpan(left.Text + (requiresSeparator ? " " : "") + right.Text);

        var i = requiresSeparator ? 2 : 1;
        Assert.Equal(left.Type, tokens[0].Type);
        Assert.Equal(left.Text, tokens[0].Text);
        Assert.Equal(right.Type, tokens[i].Type);
        Assert.Equal(right.Text, tokens[i].Text);
    }

    [Theory]
    [MemberData(nameof(GetTokenPairsData))]
    public void Tokenizer_Tokenizes_PairsSequence(TokenInfo left, TokenInfo right, bool requiresSeparator)
    {
        var tokens = ParseTokensFromSequence(left.Text + (requiresSeparator ? " " : "") + right.Text);

        var i = requiresSeparator ? 2 : 1;
        Assert.Equal(left.Type, tokens[0].Type);
        Assert.Equal(left.Text, tokens[0].Text);
        Assert.Equal(right.Type, tokens[i].Type);
        Assert.Equal(right.Text, tokens[i].Text);
    }

    public static IEnumerable<object[]> GetStringTokensData() => Tokens.Where(t => t.Type is TokenType.String or TokenType.QuotedString or TokenType.Number or TokenType.Symbol).Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(GetStringTokensData))]
    public void Tokenizer_GetString_IsCorrect(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetString();

        Assert.Equal(tokenInfo.Text, value);
    }

    public static IEnumerable<object[]> GetQuotedStringTokensData() => Tokens.Where(t => t.Type is TokenType.QuotedString).Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(GetQuotedStringTokensData))]
    public void Tokenizer_GetStringWithoutQuotes_IsCorrect(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetStringWithoutQuotes();

        Assert.Equal(tokenInfo.Text.Trim('"'), value);
    }

    public static IEnumerable<object[]> GetNumberTokensData() => Tokens.Where(t => t.Type is TokenType.Number).Select(t => new object[] { t });

    [Theory]
    [MemberData(nameof(GetNumberTokensData))]
    public void Tokenizer_GetNumber_IsCorrect(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetNumber<double>();

        Assert.Equal(Double.Parse(tokenInfo.Text, NumberStyles.Any), value);
    }

    public static IEnumerable<object[]> GetEnumTokensData() => Tokens.Where(t => t.Text.StartsWith("enum")).Select(t => new object[] { t });

    private enum EnumTest { Enum1, Enum2 };

    [Theory]
    [MemberData(nameof(GetEnumTokensData))]
    public void Tokenizer_GetEnum_IsCorrect(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetEnum<EnumTest>(ignoreCase: true);

        Assert.Equal(Enum.Parse<EnumTest>(tokenInfo.Text, ignoreCase: true), value);
    }

    [Theory]
    [MemberData(nameof(GetEnumTokensData))]
    public void Tokenizer_GetEnum_ThrowsWhenCaseDoesNotMatch(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        try
        {
            tokenizer.GetEnum<EnumTest>();
        }
        catch (Exception ex)
        {
            Assert.IsType<ArgumentException>(ex);
        }
    }
}
