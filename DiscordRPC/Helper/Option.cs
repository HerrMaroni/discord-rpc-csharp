using System;

namespace DiscordRPC.Helper;

/// <summary>
///     Represents an abstract option type that encapsulates an optional value.
/// </summary>
/// <typeparam name="T">The type of the value to be encapsulated.</typeparam>
public abstract class Option<T>
{
    /// <summary>
    ///     Gets the value encapsulated by the option.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the option is a <see cref="None{T}" />.</exception>
    public abstract T Value { get; }

    /// <summary>
    ///     Gets a value indicating whether the option contains a value.
    /// </summary>
    public abstract bool IsSome { get; }

    /// <summary>
    ///     Gets a value indicating whether the option does not contain a value.
    /// </summary>
    public abstract bool IsNone { get; }

    /// <summary>
    ///     Creates a <see cref="Some{T}" /> instance containing the provided value.
    /// </summary>
    /// <param name="value">The value to encapsulate.</param>
    /// <returns>A <see cref="Some{T}" /> instance containing the value.</returns>
    public static Option<T> Some(T value)
    {
        return new Some<T>(value);
    }

    /// <summary>
    ///     Creates a <see cref="None{T}" /> instance representing the absence of a value.
    /// </summary>
    /// <returns>A <see cref="None{T}" /> instance.</returns>
    public static Option<T> None()
    {
        return new None<T>();
    }
}

/// <summary>
///     Represents an option containing a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Some<T> : Option<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Some{T}" /> class with the specified value.
    /// </summary>
    /// <param name="value">The value to encapsulate.</param>
    public Some(T value)
    {
        Value = value;
    }

    /// <summary>
    ///     Gets the encapsulated value.
    /// </summary>
    public override T Value { get; }

    /// <summary>
    ///     Gets a value indicating that this instance contains a value.
    /// </summary>
    public override bool IsSome => true;

    /// <summary>
    ///     Gets a value indicating that this instance does not represent the absence of a value.
    /// </summary>
    public override bool IsNone => false;
}

/// <summary>
///     Represents an option with no value.
/// </summary>
/// <typeparam name="T">The type of the absent value.</typeparam>
public class None<T> : Option<T>
{
    /// <summary>
    ///     Gets the value of the option, which in this case throws an exception because there is no value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown to indicate that no value is present.</exception>
    public override T Value => throw new InvalidOperationException("No value present");

    /// <summary>
    ///     Gets a value indicating that this instance does not contain a value.
    /// </summary>
    public override bool IsSome => false;

    /// <summary>
    ///     Gets a value indicating that this instance represents the absence of a value.
    /// </summary>
    public override bool IsNone => true;
}