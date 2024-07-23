using System.Text.Json.Serialization;

namespace DiscordRPC.RPC.Payload;

/// <summary>
///     Base Payload that is received by both client and server
/// </summary>
internal abstract class Payload
{
    protected Payload()
    {
    }

    protected Payload(long nonce)
    {
        Nonce = nonce.ToString();
    }

    /// <summary>
    ///     The type of payload
    /// </summary>
    [JsonPropertyName("cmd")]
    [JsonConverter(typeof(JsonStringEnumConverter<Command>))]
    public Command Command { get; set; }

    /// <summary>
    ///     A incremental value to help identify payloads
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }

    public override string ToString()
    {
        return $"Payload || Command: {Command}, Nonce: {Nonce}";
    }
}