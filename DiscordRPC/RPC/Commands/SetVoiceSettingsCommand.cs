using System.Text.Json;
using System.Text.Json.Serialization;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class SetVoiceSettingsCommand(VoiceSettings voiceSettings) : ICommand
{
    public VoiceSettings VoiceSettings { get; set; } = voiceSettings;

    public Payload.Payload PreparePayload(long nonce)
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Serialize the VoiceSettings object to JSON
        var voiceSettingsJson = JsonSerializer.SerializeToDocument(VoiceSettings, options);

        return new ArgumentPayload<SetVoiceSettingsCommand>(nonce)
        {
            Command = Command.SET_VOICE_SETTINGS,
            Arguments = voiceSettingsJson
        };
    }
}