using System;
using System.Text.Json.Serialization;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class AuthenticateCommand : ICommand
{
    /// <summary>
    ///     OAuth2 access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    public Payload.Payload PreparePayload(long nonce)
    {
        return new ArgumentPayload<AuthenticateCommand>(this, nonce)
        {
            Command = Command.AUTHENTICATE
        };
    }
}

[Serializable]
internal class AuthenticateResponse
{
    /// <summary>
    ///     the authed user
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; }

    /// <summary>
    ///     authorized scopes
    /// </summary>
    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; }

    /// <summary>
    ///     expiration date of OAuth2 token
    /// </summary>
    [JsonPropertyName("expires")]
    public DateTime Expires { get; set; }

    /// <summary>
    ///     expiration date of OAuth2 token
    /// </summary>
    [JsonPropertyName("application")]
    public Application Application { get; set; }
}