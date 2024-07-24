﻿using DiscordRPC.IO;
using DiscordRPC.Message;
using DiscordRPC.RPC.Commands;
using DiscordRPC.RPC.Payload;
using System.Text.Json.Serialization;

namespace DiscordRPC.Helper
{
    [
        /* Commands Context */
        JsonSerializable(typeof(CloseCommand)),
        JsonSerializable(typeof(PresenceCommand)),
        JsonSerializable(typeof(RespondCommand)),
        JsonSerializable(typeof(SubscribeCommand)),

        /* Payload Context */
        JsonSerializable(typeof(EventPayload)),
        JsonSerializable(typeof(ClosePayload)),
        JsonSerializable(typeof(IPayload)),
        JsonSerializable(typeof(ArgumentPayload<CloseCommand>)),
        JsonSerializable(typeof(ArgumentPayload<PresenceCommand>)),
        JsonSerializable(typeof(ArgumentPayload<RespondCommand>)),
        
        /* Respond Context */
        JsonSerializable(typeof(RichPresenceResponse)),

        /* IO Context */
        JsonSerializable(typeof(Handshake)),

        /* Message Context */
        JsonSerializable(typeof(CloseMessage)),
        JsonSerializable(typeof(ConnectionEstablishedMessage)),
        JsonSerializable(typeof(ConnectionFailedMessage)),
        JsonSerializable(typeof(ErrorMessage)),
        JsonSerializable(typeof(IMessage)),
        JsonSerializable(typeof(JoinMessage)),
        JsonSerializable(typeof(JoinRequestMessage)),
        JsonSerializable(typeof(PresenceMessage)),
        JsonSerializable(typeof(ReadyMessage)),
        JsonSerializable(typeof(SpectateMessage)),
        JsonSerializable(typeof(SubscribeMessage)),
        JsonSerializable(typeof(UnsubscribeMessage)),
    ]
    internal partial class JsonSerializationContext : JsonSerializerContext { }
}