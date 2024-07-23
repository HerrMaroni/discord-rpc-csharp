using System;
using DiscordRPC.RPC.Commands;

namespace DiscordRPC.Message;

/// <summary>
///     Representation of the message received by discord when an authentication response has been received.
/// </summary>
public class AuthenticateMessage : Message
{
    internal AuthenticateMessage(AuthenticateResponse auth)
    {
        if (auth == null)
        {
            User = null;
            Scopes = null;
            Expires = DateTime.MinValue;
            Application = null;
        }
        else
        {
            User = auth.User;
            Scopes = auth.Scopes;
            Expires = auth.Expires;
            Application = auth.Application;
        }
    }

    /// <summary>
    ///     The type of message received from discord
    /// </summary>
    public override MessageType Type => MessageType.Authenticate;

    /// <summary>
    ///     the authed user
    /// </summary>
    public User User { get; }

    /// <summary>
    ///     authorized scopes
    /// </summary>
    public string[] Scopes { get; }

    /// <summary>
    ///     expiration date of OAuth2 token
    /// </summary>
    public DateTime Expires { get; }

    /// <summary>
    ///     expiration date of OAuth2 token
    /// </summary>
    public Application Application { get; }
}