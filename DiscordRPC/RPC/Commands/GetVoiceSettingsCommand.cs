using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC.Commands;

internal class GetVoiceSettingsCommand : ICommand
{
    public Payload.Payload PreparePayload(long nonce)
    {
        return new ArgumentPayload<GetVoiceSettingsCommand>(this, nonce)
        {
            Command = Command.GET_VOICE_SETTINGS,
        };
    }
}