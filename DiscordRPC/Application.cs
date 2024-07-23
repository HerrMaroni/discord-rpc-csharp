using System.Text.Json.Serialization;

namespace DiscordRPC;

/// <summary>
///     Object representing a Discord application
/// </summary>
public class Application
{
    /// <summary>
    ///     Gets or sets the application description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the hash of the icon.
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    /// <summary>
    ///     Gets or sets the application client ID.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public ulong ID { get; set; }

    /// <summary>
    ///     Gets or sets the array of RPC origin URLs.
    /// </summary>
    [JsonPropertyName("rpc_origins")]
    public string[] RpcOrigins { get; set; }

    /// <summary>
    ///     Gets or sets the application name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
}