using System.Buffers;
using System.Globalization;

namespace Lionware.Text;

public sealed class sho_uld
{
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
    [MemberData(nameof(TokenizerData.GetTokensData), MemberType = typeof(TokenizerData))]
    public void Tokenize_token_span(TokenInfo tokenInfo)
    {
        var token = ParseTokensFromSpan(tokenInfo.Text).First();

        Assert.Equal(tokenInfo.Type, token.Type);
        Assert.Equal(tokenInfo.Text, token.Text);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetTokensData), MemberType = typeof(TokenizerData))]
    public void Tokenize_token_sequence(TokenInfo tokenInfo)
    {
        var token = ParseTokensFromSequence(tokenInfo.Text).First();

        Assert.Equal(tokenInfo.Type, token.Type);
        Assert.Equal(tokenInfo.Text, token.Text);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetTokenPairsData), MemberType = typeof(TokenizerData))]
    public void tokenize_span_pair(TokenInfo left, TokenInfo right, bool requiresSeparator)
    {
        var tokens = ParseTokensFromSpan(left.Text + (requiresSeparator ? " " : "") + right.Text);

        var i = requiresSeparator ? 2 : 1;
        Assert.Equal(left.Type, tokens[0].Type);
        Assert.Equal(left.Text, tokens[0].Text);
        Assert.Equal(right.Type, tokens[i].Type);
        Assert.Equal(right.Text, tokens[i].Text);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetTokenPairsData), MemberType = typeof(TokenizerData))]
    public void Tokenize_sequence_pair(TokenInfo left, TokenInfo right, bool requiresSeparator)
    {
        var tokens = ParseTokensFromSequence(left.Text + (requiresSeparator ? " " : "") + right.Text);

        var i = requiresSeparator ? 2 : 1;
        Assert.Equal(left.Type, tokens[0].Type);
        Assert.Equal(left.Text, tokens[0].Text);
        Assert.Equal(right.Type, tokens[i].Type);
        Assert.Equal(right.Text, tokens[i].Text);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetStringTokensData), MemberType = typeof(TokenizerData))]
    public void Get_string(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetString();

        Assert.Equal(tokenInfo.Text, value);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetQuotedStringTokensData), MemberType = typeof(TokenizerData))]
    public void Get_string_without_quotes(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetStringWithoutQuotes();

        Assert.Equal(tokenInfo.Text.Trim('"'), value);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetNumberTokensData), MemberType = typeof(TokenizerData))]
    public void Get_number(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetNumber<double>();

        Assert.Equal(Double.Parse(tokenInfo.Text, NumberStyles.Any), value);
    }

    private enum EnumTest { Enum1, Enum2 };

    [Theory]
    [MemberData(nameof(TokenizerData.GetEnumTokensData), MemberType = typeof(TokenizerData))]
    public void Get_enum(TokenInfo tokenInfo)
    {
        var tokenizer = new Tokenizer(tokenInfo.Text);
        tokenizer.Read();
        var value = tokenizer.GetEnum<EnumTest>(ignoreCase: true);

        Assert.Equal(Enum.Parse<EnumTest>(tokenInfo.Text, ignoreCase: true), value);
    }

    [Theory]
    [MemberData(nameof(TokenizerData.GetEnumTokensData), MemberType = typeof(TokenizerData))]
    public void Throw_when_enum_case_does_not_match(TokenInfo tokenInfo)
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

file sealed class MemorySegment<T> : ReadOnlySequenceSegment<T>
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
public sealed record class TokenInfo(TokenType Type, string Text);

public sealed class TokenizerData
{
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

    public static IEnumerable<object[]> GetStringTokensData() => Tokens.Where(t => t.Type is TokenType.String or TokenType.QuotedString or TokenType.Number or TokenType.Symbol).Select(t => new object[] { t });

    public static IEnumerable<object[]> GetQuotedStringTokensData() => Tokens.Where(t => t.Type is TokenType.QuotedString).Select(t => new object[] { t });

    public static IEnumerable<object[]> GetNumberTokensData() => Tokens.Where(t => t.Type is TokenType.Number).Select(t => new object[] { t });

    public static IEnumerable<object[]> GetEnumTokensData() => Tokens.Where(t => t.Text.StartsWith("enum")).Select(t => new object[] { t });
}
