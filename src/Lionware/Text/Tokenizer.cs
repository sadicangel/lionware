using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Lionware.Text;

/// <summary>
/// The type of token parsed by <see cref="Tokenizer"/>.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// There is no value.
    /// </summary>
    None,
    /// <summary>
    /// The token is the end of the input stream.
    /// </summary>
    EOF,
    /// <summary>
    /// The token is the end of line.
    /// </summary>
    LineBreak,
    /// <summary>
    /// The token is white space.
    /// </summary>
    Whitespace,
    /// <summary>
    /// The token is a quoted word.
    /// </summary>
    QuotedString,
    /// <summary>
    /// The token is a word.
    /// </summary>
    String,
    /// <summary>
    /// The token is a number.
    /// </summary>
    Number,
    /// <summary>
    /// The token is a symbol.
    /// </summary>
    Symbol
}

/// <summary>
/// Parses input text into tokens categorized by <see cref="TokenType"/>.
/// </summary>
public ref struct Tokenizer
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ReadOnlySpan<char> _buffer;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ReadOnlySequence<char> _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer" /> struct.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    public Tokenizer(ReadOnlySpan<char> text)
    {
        _buffer = text;
        _sequence = ReadOnlySequence<char>.Empty;
        IsValueSpan = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tokenizer" /> struct.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    public Tokenizer(ReadOnlySequence<char> text)
    {
        _buffer = ReadOnlySpan<char>.Empty;
        _sequence = text;
        IsValueSpan = false;

        if (text.IsSingleSegment)
        {
            _buffer = text.FirstSpan;
            _sequence = ReadOnlySequence<char>.Empty;
            IsValueSpan = true;
        }
    }

    /// <summary>
    /// Gets a value that indicates which Value property to use to get the token value.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="ValueSpan"/> should be used to get the
    /// token value; <see langword="false"/> if <see cref="ValueSequence"/> should be used instead.
    /// </returns>
    public bool IsValueSpan { get; private set; }

    /// <summary>
    /// Gets the type of the last processed token.
    /// </summary>
    public TokenType TokenType { get; private set; }

    /// <summary>
    /// Gets the raw value of the last processed token as a <see cref="ReadOnlySequence{T}"/> slice
    /// of the input payload, only if the token is contained within multiple segments.
    /// </summary>
    public ReadOnlySpan<char> ValueSpan { get; private set; }

    /// <summary>
    /// Gets the raw value of the last processed token as a <see cref="ReadOnlySpan{T}"/> slice
    /// of the input payload, if the token fits in a single segment or if the reader
    /// was constructed with a payload contained in a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySequence<char> ValueSequence { get; private set; }

    /// <summary>
    /// Gets the current position within the input text.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the current line number.
    /// </summary>
    public int Line { get; private set; } = 1;

    /// <summary>
    /// Gets the current 1-based character index.
    /// </summary>
    public int Character { get; private set; } = 1;

    /// <summary>
    /// Reads the next token from input source.
    /// </summary>
    /// <returns><see langword="true"/> if the token was read successfully, else <see langword="false"/>.</returns>
    public bool Read()
    {
        if (TokenType is TokenType.EOF)
            return false;

        int length;
        if (_sequence.IsEmpty)
        {
            var buffer = _buffer[Position..];
            (TokenType, length) = NextToken(buffer);
            buffer = buffer[..length];
            ValueSpan = buffer;
        }
        else
        {
            var sequence = _sequence.Slice(Position);
            (TokenType, length) = NextToken(sequence);
            sequence = sequence.Slice(0, length);
            IsValueSpan = sequence.IsSingleSegment;
            if (IsValueSpan)
            {
                ValueSpan = sequence.FirstSpan;
                ValueSequence = ReadOnlySequence<char>.Empty;
            }
            else
            {
                ValueSpan = ReadOnlySpan<char>.Empty;
                ValueSequence = sequence;
            }
        }
        Position += length;
        Character += length;
        if (TokenType is TokenType.LineBreak)
        {
            Line++;
            Character = 1;
        }
        return true;
    }

    private static (TokenType type, int length) NextToken(ReadOnlySpan<char> text)
    {
        return text switch
        {
            // EOF
            [] => (TokenType.EOF, 0),

            // LineBreak
            ['\n', ..] => (TokenType.LineBreak, 1),
            ['\r', '\n', ..] => (TokenType.LineBreak, 2),

            // Whitespace
            [var w, ..] when Char.IsWhiteSpace(w) => (TokenType.Whitespace, ReadWhile(text, Char.IsWhiteSpace)),

            // QuotedString
            ['"', ..] => (TokenType.QuotedString, ReadWhile(text, c => c is not '"', consumeLast: true)),

            // String
            [var s, ..] when Char.IsLetter(s) => (TokenType.String, ReadWhile(text, Char.IsLetterOrDigit)),

            // Number
            ['0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9', ..] => (TokenType.Number, ReadNumber(text)),
            ['-', '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9', ..] => (TokenType.Number, ReadNumber(text)),

            // Symbol
            _ => (TokenType.Symbol, 1)
        };

        static int ReadWhile(ReadOnlySpan<char> text, Func<char, bool> predicate, bool consumeLast = false)
        {
            var position = 0;
            do
            {
                position++;
            }
            while (position < text.Length && predicate(text[position]));

            if (consumeLast)
                position++;

            return position;
        }

        static int ReadNumber(ReadOnlySpan<char> text)
        {
            var position = ReadWhile(text, Char.IsDigit);

            text = text[position..];

            if (text is ['.', var d, ..] && Char.IsDigit(d))
            {
                var length = 1 + ReadWhile(text[1..], Char.IsDigit);
                position += length;
                text = text[length..];
            }

            if (text is ['E' or 'e', ..])
            {
                position += text switch
                {
                    ['E' or 'e', '+' or '-', var e, ..] when Char.IsDigit(e) => 2 + ReadWhile(text[2..], Char.IsDigit),
                    ['E' or 'e', var e, ..] when Char.IsDigit(e) => 1 + ReadWhile(text[1..], Char.IsDigit),
                    _ => 0
                };
            }

            return position;
        }
    }

    private static (TokenType type, int length) NextToken(ReadOnlySequence<char> text)
    {
        var length = Math.Min(4, (int)text.Length);
        Span<char> textSpan = stackalloc char[length];
        text.Slice(0, length).CopyTo(textSpan);
        return textSpan switch
        {
            // EOF
            [] => (TokenType.EOF, 0),

            // LineBreak
            ['\n', ..] => (TokenType.LineBreak, 1),
            ['\r', '\n', ..] => (TokenType.LineBreak, 2),

            // Whitespace
            [var w, ..] when Char.IsWhiteSpace(w) => (TokenType.Whitespace, ReadWhile(text, Char.IsWhiteSpace)),

            // QuotedString
            ['"', ..] => (TokenType.QuotedString, ReadWhile(text, c => c is not '"', consumeLast: true)),

            // String
            [var s, ..] when Char.IsLetter(s) => (TokenType.String, ReadWhile(text, Char.IsLetterOrDigit)),

            // Number
            ['0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9', ..] => (TokenType.Number, ReadNumber(text)),
            ['-', '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9', ..] => (TokenType.Number, ReadNumber(text)),

            // Symbol
            _ => (TokenType.Symbol, 1)
        };

        static int ReadWhile(ReadOnlySequence<char> text, Func<char, bool> predicate, bool consumeLast = false)
        {
            var reader = new SequenceReader<char>(text);
            var position = 1;
            reader.Advance(1);
            while (reader.TryPeek(out var current) && predicate(current))
            {
                position++;
                reader.Advance(1);
            }

            if (consumeLast)
            {
                position++;
                reader.Advance(1);
            }

            return position;
        }

        static int ReadNumber(ReadOnlySequence<char> text)
        {
            var reader = new SequenceReader<char>(text);

            var position = 1;
            reader.Advance(1);
            char curr = '\0';
            while (reader.TryPeek(out curr) && Char.IsDigit(curr))
            {
                position++;
                reader.Advance(1);
            }

            if (reader.TryPeek(out curr) && curr is '.' && reader.TryPeek(1, out char next) && Char.IsDigit(next))
            {
                position++;
                reader.Advance(1);
                while (reader.TryPeek(out curr) && Char.IsDigit(curr))
                {
                    position++;
                    reader.Advance(1);
                }
            }

            if (reader.TryPeek(out curr) && curr is 'E' or 'e')
            {
                int offset = 0;
                if (reader.TryPeek(1, out next) && Char.IsDigit(next))
                    offset = 1;
                else if (reader.TryPeek(1, out next) && next is '+' or '-' && reader.TryPeek(2, out var after) && Char.IsDigit(after))
                    offset = 2;
                if (offset > 0)
                {
                    position += offset;
                    reader.Advance(offset);
                    while (reader.TryPeek(out curr) && Char.IsDigit(curr))
                    {
                        position++;
                        reader.Advance(1);
                    }
                }
            }
            return position;
        }
    }

    /// <summary>
    /// Gets the value of the last processed token as a <see cref="string"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> equivalent of the last processed token.
    /// </returns>
    public readonly string GetString() => IsValueSpan ? ValueSpan.ToString() : ValueSequence.ToString();

    /// <summary>
    /// Gets the value of the last processed token as a <see cref="string"/> without any surrounding quotes.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> equivalent of the last processed token without any surrounding quotes.
    /// </returns>
    public readonly string GetStringWithoutQuotes()
    {
        if (TokenType is not TokenType.QuotedString)
            return GetString();

        if (IsValueSpan)
            return ValueSpan[1..^1].ToString();
        else
            return ValueSequence.Slice(1, ValueSequence.Length - 2).ToString();
    }

    /// <summary>
    /// Gets the value of the last processed token as a number of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of number.</typeparam>
    /// <returns>
    /// A number of type <typeparamref name="T"/> equivalent of the last processed token.
    /// </returns>
    public readonly T GetNumber<T>() where T : INumber<T>
    {
        char[]? array = null;
        try
        {
            scoped ReadOnlySpan<char> buffer;
            if (IsValueSpan)
            {
                buffer = ValueSpan;
            }
            else
            {
                var length = checked((int)ValueSequence.Length);
                Span<char> temp = length < 128
                    ? stackalloc char[(int)ValueSequence.Length]
                    : (array = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);
                ValueSequence.CopyTo(temp);
                buffer = temp;
            }
            return T.Parse(buffer, NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (array is not null)
                ArrayPool<char>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Gets the value of the last processed token as a <see cref="Enum"/> of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <returns>
    /// An <see cref="Enum"/> of type <typeparamref name="T" /> equivalent of the last processed token.
    /// </returns>
    public readonly T GetEnum<T>() where T : struct, Enum => GetEnum<T>(ignoreCase: false);

    /// <summary>
    /// Gets the value of the last processed token as a <see cref="Enum"/> of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="ignoreCase"><see langword="true"/> to ignore case; <see langword="false"/> to regard case.</param>
    /// <returns>
    /// An <see cref="Enum"/> of type <typeparamref name="T" /> equivalent of the last processed token.
    /// </returns>
    public readonly T GetEnum<T>(bool ignoreCase) where T : struct, Enum
    {
        char[]? array = null;
        try
        {
            scoped ReadOnlySpan<char> buffer;
            if (IsValueSpan)
            {
                buffer = ValueSpan;
            }
            else
            {
                var length = checked((int)ValueSequence.Length);
                Span<char> temp = length < 128
                    ? stackalloc char[(int)ValueSequence.Length]
                    : (array = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);
                ValueSequence.CopyTo(temp);
                buffer = temp;
            }
            return Enum.Parse<T>(buffer, ignoreCase);
        }
        finally
        {
            if (array is not null)
                ArrayPool<char>.Shared.Return(array);
        }
    }
}