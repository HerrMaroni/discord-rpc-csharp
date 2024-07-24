using System.Text.Json.Serialization;
using DiscordRPC.IO;
using DiscordRPC.Message;
using DiscordRPC.RPC.Commands;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.Helper;

#region Commands
[JsonSerializable(typeof(AuthorizeCommand))]
[JsonSerializable(typeof(AuthenticateCommand))]
[JsonSerializable(typeof(CloseCommand))]
[JsonSerializable(typeof(PresenceCommand))]
[JsonSerializable(typeof(RespondCommand))]
[JsonSerializable(typeof(GetVoiceSettingsCommand))]
[JsonSerializable(typeof(SetVoiceSettingsCommand))]
[JsonSerializable(typeof(SubscribeCommand))]
#endregion

#region Payload
[JsonSerializable(typeof(EventPayload))]
[JsonSerializable(typeof(ClosePayload))]
[JsonSerializable(typeof(Payload))]
[JsonSerializable(typeof(ArgumentPayload<AuthorizeCommand>))]
[JsonSerializable(typeof(ArgumentPayload<AuthenticateCommand>))]
[JsonSerializable(typeof(ArgumentPayload<CloseCommand>))]
[JsonSerializable(typeof(ArgumentPayload<PresenceCommand>))]
[JsonSerializable(typeof(ArgumentPayload<RespondCommand>))]
[JsonSerializable(typeof(ArgumentPayload<GetVoiceSettingsCommand>))]
[JsonSerializable(typeof(ArgumentPayload<SetVoiceSettingsCommand>))]
#endregion

#region Response
[JsonSerializable(typeof(AuthorizeResponse))]
[JsonSerializable(typeof(AuthenticateResponse))]
[JsonSerializable(typeof(VoiceSettings))]
[JsonSerializable(typeof(RichPresenceResponse))]
#endregion

#region IO
[JsonSerializable(typeof(Handshake))]
#endregion

#region Message
[JsonSerializable(typeof(AuthorizeMessage))]
[JsonSerializable(typeof(AuthenticateMessage))]
[JsonSerializable(typeof(CloseMessage))]
[JsonSerializable(typeof(ConnectionEstablishedMessage))]
[JsonSerializable(typeof(ConnectionFailedMessage))]
[JsonSerializable(typeof(ErrorMessage))]
[JsonSerializable(typeof(Message.Message))]
[JsonSerializable(typeof(JoinMessage))]
[JsonSerializable(typeof(JoinRequestMessage))]
[JsonSerializable(typeof(PresenceMessage))]
[JsonSerializable(typeof(ReadyMessage))]
[JsonSerializable(typeof(SpectateMessage))]
[JsonSerializable(typeof(SubscribeMessage))]
[JsonSerializable(typeof(UnsubscribeMessage))]
#endregion
internal partial class JsonSerializationContext : JsonSerializerContext
{
}