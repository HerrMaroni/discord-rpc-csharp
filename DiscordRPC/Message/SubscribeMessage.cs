using DiscordRPC.RPC.Payload;

namespace DiscordRPC.Message;

/// <summary>
///     Called as validation of a subscribe
/// </summary>
public class SubscribeMessage : Message
{
    internal SubscribeMessage(ServerEvent evt)
    {
        switch (evt)
        {
            default:
            case ServerEvent.ACTIVITY_JOIN:
                Event = EventType.Join;
                break;

            case ServerEvent.ACTIVITY_JOIN_REQUEST:
                Event = EventType.JoinRequest;
                break;

            case ServerEvent.ACTIVITY_SPECTATE:
                Event = EventType.Spectate;
                break;
        }
    }

    /// <summary>
    ///     The type of message received from discord
    /// </summary>
    public override MessageType Type => MessageType.Subscribe;

    /// <summary>
    ///     The event that was subscribed too.
    /// </summary>
    public EventType Event { get; internal set; }
}