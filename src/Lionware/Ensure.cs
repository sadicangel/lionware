using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lionware;

/// <summary>
/// Provides runtime checks for arguments.
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is <see langword="null" />.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNull([NotNull] object? argument, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null) =>
        ArgumentNullException.ThrowIfNull(argument, argumentExpression);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is <see langword="null" />.
    /// </summary>
    /// <param name="argument">The pointer argument to validate as non-null.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"/>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void NotNull([NotNull] void* argument, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null) =>
        ArgumentNullException.ThrowIfNull(argument, argumentExpression);

    /// <summary>
    /// Throws an exception if <paramref name="argument"/> is <see langword="null" />.
    /// </summary>
    /// <param name="argument">The string argument to validate as non-null and non-empty.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NotNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null) =>
        ArgumentException.ThrowIfNullOrEmpty(argument, argumentExpression);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is less than or equal to <paramref name="comparand"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="argument">The argument to validate as greater than or equal to <paramref name="comparand"/>.</param>
    /// <param name="comparand">The value to which <paramref name="argument"/> is compared.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GreaterThan<T>(T argument, T comparand, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(comparand) <= 0)
            throw new ArgumentOutOfRangeException(argumentExpression, $"{argumentExpression} ({argument}) must be greater than {comparand}.");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is less than <paramref name="comparand"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="argument">The argument to validate as greater than or equal to <paramref name="comparand"/>.</param>
    /// <param name="comparand">The value to which <paramref name="argument"/> is compared.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GreaterThanOrEqualTo<T>(T argument, T comparand, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(comparand) < 0)
            throw new ArgumentOutOfRangeException(argumentExpression, $"{argumentExpression} ({argument}) must be greater than or equal to {comparand}.");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is greater than or equal to <paramref name="comparand"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="argument">The argument to validate as greater than or equal to <paramref name="comparand"/>.</param>
    /// <param name="comparand">The value to which <paramref name="argument"/> is compared.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LessThan<T>(T argument, T comparand, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(comparand) >= 0)
            throw new ArgumentOutOfRangeException(argumentExpression, $"{argumentExpression} ({argument}) must be less than {comparand}.");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is greater than <paramref name="comparand"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="argument">The argument to validate as greater than or equal to <paramref name="comparand"/>.</param>
    /// <param name="comparand">The value to which <paramref name="argument"/> is compared.</param>
    /// <param name="argumentExpression">The parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LessThanOrEqualTo<T>(T argument, T comparand, [CallerArgumentExpression((nameof(argument)))] string? argumentExpression = null)
        where T : IComparable<T>
    {
        if (argument.CompareTo(comparand) > 0)
            throw new ArgumentOutOfRangeException(argumentExpression, $"{argumentExpression} ({argument}) must be less than or equal to {comparand}.");
    }
}
