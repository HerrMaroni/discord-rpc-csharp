using System;

namespace DiscordRPC.Exceptions;

/// <summary>
///     The exception that is thrown when an error occurs while communicating with a pipe or when a connection attempt
///     fails.
/// </summary>
[Obsolete("Not actually used anywhere")]
public class InvalidPipeException : Exception
{
    internal InvalidPipeException(string message) : base(message)
    {
    }
}