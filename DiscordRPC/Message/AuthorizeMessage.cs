using DiscordRPC.RPC.Commands;

namespace DiscordRPC.Message;

/// <summary>
///     Representation of the message received by discord when an authorization response has been received.
/// </summary>
public class AuthorizeMessage : Message
{
    internal AuthorizeMessage(AuthorizeResponse auth)
    {
        Code = auth == null ? "" : auth.Code;
    }

    /// <summary>
    ///     The type of message received from discord
    /// </summary>
    public override MessageType Type => MessageType.Authorize;

    /// <summary>
    ///     The OAuth2 authorization code
    /// </summary>
    public string Code { get; internal set; }
}