using System;
using System.Text.Json.Serialization;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class AuthorizeCommand : ICommand
{
    /// <summary>
    ///     OAuth2 application id
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    /// <summary>
    ///     scopes to authorize
    /// </summary>
    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }

    public Payload.Payload PreparePayload(long nonce)
    {
        return new ArgumentPayload<AuthorizeCommand>(this, nonce)
        {
            Command = Command.AUTHORIZE
        };
    }
}

[Serializable]
internal class AuthorizeResponse
{
    /// <summary>
    ///     The OAuth2 authorization code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }
}