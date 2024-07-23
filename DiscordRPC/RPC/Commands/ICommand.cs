namespace DiscordRPC.RPC.Commands;

internal interface ICommand
{
    Payload.Payload PreparePayload(long nonce);
}