namespace DiscordRPC.Message;

/// <summary>
///     Represents a message containing voice settings received from Discord.
/// </summary>
public class VoiceSettingsMessage : Message
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VoiceSettingsMessage" /> class with specified voice settings.
    /// </summary>
    /// <param name="voiceSettings">The voice settings received from Discord.</param>
    internal VoiceSettingsMessage(VoiceSettings voiceSettings)
    {
        VoiceSettings = voiceSettings;
    }

    /// <summary>
    ///     The type of message received from discord
    /// </summary>
    public override MessageType Type => MessageType.VoiceSettings;

    /// <summary>
    ///     Gets or sets the voice settings received from Discord.
    /// </summary>
    public VoiceSettings VoiceSettings { get; set; }
}