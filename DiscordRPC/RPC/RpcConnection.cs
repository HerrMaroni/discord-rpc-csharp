using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DiscordRPC.Helper;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using DiscordRPC.RPC.Commands;
using DiscordRPC.RPC.Payload;

namespace DiscordRPC.RPC;

/// <summary>
///     Communicates between the client and discord through RPC
/// </summary>
internal class RpcConnection : IDisposable
{
    /// <summary>
    ///     Version of the RPC Protocol
    /// </summary>
    private const int Version = 1;

    /// <summary>
    ///     The rate of poll to the discord pipe.
    /// </summary>
    private const int PollRate = 1000;

    /// <summary>
    ///     Should we send a null presence on the farewells?
    /// </summary>
    private const bool ClearOnShutdown = true;

    /// <summary>
    ///     Should we work in a lock step manner? This option is semi-obsolete and may not work as expected.
    /// </summary>
    private const bool LockStep = false;

    private ILogger _logger;

    /// <summary>
    ///     Creates a new instance of the RPC.
    /// </summary>
    /// <param name="applicationID">The ID of the Discord App</param>
    /// <param name="processID">The ID of the currently running process</param>
    /// <param name="targetPipe">The target pipe to connect too</param>
    /// <param name="client">The pipe client we shall use.</param>
    /// <param name="maxRxQueueSize">The maximum size of the out queue</param>
    /// <param name="maxTxQueueSize">The maximum size of the in queue</param>
    public RpcConnection(string applicationID, int processID, int targetPipe, INamedPipeClient client,
        uint maxRxQueueSize = 128, uint maxTxQueueSize = 512)
    {
        _applicationID = applicationID;
        _processID = processID;
        _targetPipe = targetPipe;
        _namedPipe = client;
        ShutdownOnly = true;

        // Assign a default logger
        Logger = new ConsoleLogger();

        _delay = new BackoffDelay(500, 60 * 1000);
        _maxTxQueueSize = maxTxQueueSize;
        _txQueue = new Queue<ICommand>((int)_maxTxQueueSize + 1);

        _maxRxQueueSize = maxRxQueueSize;
        _rxQueue = new Queue<Message.Message>((int)_maxRxQueueSize + 1);

        _nonce = 0;
    }

    /// <summary>
    ///     The logger used by the RPC connection
    /// </summary>
    public ILogger Logger
    {
        get => _logger;
        set
        {
            _logger = value;
            if (_namedPipe != null)
                _namedPipe.Logger = value;
        }
    }

    /// <summary>
    ///     Called when a message is received from the RPC and is about to be enqueued. This is cross-thread and will execute
    ///     on the RPC thread.
    /// </summary>
    public event RpcMessageEvent OnRpcMessage;

    private long GetNextNonce()
    {
        _nonce += 1;
        return _nonce;
    }

    /// <summary>
    ///     Main thread loop
    /// </summary>
    private void MainLoop()
    {
        // initialize the pipe
        Logger.Info("RPC Connection Started");
        if (Logger.Level <= LogLevel.Trace)
        {
            Logger.Trace("============================");
            Logger.Trace("Assembly:             " + Assembly.GetAssembly(typeof(RichPresence))?.FullName);
            Logger.Trace("Pipe:                 " + _namedPipe.GetType().FullName);
            Logger.Trace("Platform:             " + Environment.OSVersion);
            Logger.Trace("applicationID:        " + _applicationID);
            Logger.Trace("targetPipe:           " + _targetPipe);
            Logger.Trace("POLL_RATE:            " + PollRate);
            Logger.Trace("_maxRtQueueSize:      " + _maxTxQueueSize);
            Logger.Trace("_maxRxQueueSize:      " + _maxRxQueueSize);
            Logger.Trace("============================");
        }

        // Forever trying to connect unless the abort signal is sent
        // Keep Alive Loop
        while (!_aborting && !_shutdown)
            try
            {
                // Wrap everything up in a try get
                // Dispose of the pipe if we have any (could be broken)
                if (_namedPipe == null)
                {
                    Logger.Error("Something bad has happened with our pipe client!");
                    _aborting = true;
                    return;
                }

                // Connect to a new pipe
                Logger.Trace("Connecting to the pipe through the {0}", _namedPipe.GetType().FullName);
                if (_namedPipe.Connect(_targetPipe))
                {
                    #region Connected

                    // We connected to a pipe! Reset the delay
                    Logger.Trace("Connected to the pipe. Attempting to establish handshake...");
                    EnqueueMessage(new ConnectionEstablishedMessage { ConnectedPipe = _namedPipe.ConnectedPipe });

                    // Attempt to establish a handshake
                    EstablishHandshake();
                    Logger.Trace("Connection Established. Starting reading loop...");

                    // Continuously iterate, waiting for the frame
                    // We want to only stop reading if the inside tells us (mainloop), if we are aborting (abort) or the pipe disconnects
                    // We don't want to exit on a shutdown, as we still have information
                    var mainloop = true;
                    while (mainloop && !_aborting && !_shutdown && _namedPipe.IsConnected)
                    {
                        #region Read Loop

                        // Iterate over every frame we have queued up, processing its contents
                        if (_namedPipe.ReadFrame(out var frame))
                        {
                            #region Read Payload

                            Logger.Trace("Read Payload: {0}", frame.Opcode);

                            // Do some basic processing on the frame
                            switch (frame.Opcode)
                            {
                                // We have been told by discord to close, so we will consider it an abort
                                case Opcode.Close:

                                    var close = frame.GetObject<ClosePayload>();
                                    Logger.Warning("We have been told to terminate by discord: ({0}) {1}", close.Code,
                                        close.Reason);
                                    EnqueueMessage(new CloseMessage { Code = close.Code, Reason = close.Reason });
                                    mainloop = false;
                                    break;

                                // We have pinged, so we will flip it and respond back with pong
                                case Opcode.Ping:
                                    Logger.Trace("PING");
                                    frame.Opcode = Opcode.Pong;
                                    _namedPipe.WriteFrame(frame);
                                    break;

                                // We have ponged? I have no idea if Discord actually sends ping/pongs.
                                case Opcode.Pong:
                                    Logger.Trace("PONG");
                                    break;

                                // A frame has been sent, we should deal with that
                                case Opcode.Frame:
                                    if (_shutdown)
                                    {
                                        // We are shutting down, so skip it
                                        Logger.Warning("Skipping frame because we are shutting down.");
                                        break;
                                    }

                                    if (frame.Data == null)
                                    {
                                        // We have invalid data, that's not good.
                                        Logger.Error(
                                            "We received no data from the frame so we cannot get the event payload!");
                                        break;
                                    }

                                    // We have a frame, so we are going to process the payload and add it to the stack
                                    EventPayload response = null;
                                    try
                                    {
                                        response = frame.GetObject<EventPayload>();
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error("Failed to parse event! {0}", e.Message);
                                        Logger.Error("Data: {0}", frame.Message);
                                    }

                                    try
                                    {
                                        if (response != null) ProcessFrame(response);
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.Error("Failed to process event! {0}", e.Message);
                                        Logger.Error("Data: {0}", frame.Message);
                                    }

                                    break;

                                default:
                                case Opcode.Handshake:
                                    // We have an invalid opcode, better terminate to be safe
                                    Logger.Error("Invalid opcode: {0}", frame.Opcode);
                                    mainloop = false;
                                    break;
                            }

                            #endregion
                        }

                        if (_aborting || !_namedPipe.IsConnected) continue;
                        // Process the entire command queue we have left
                        ProcessCommandQueue();

                        // Wait for some time, or until a command has been queued up
                        _queueUpdatedEvent.WaitOne(PollRate);

                        #endregion
                    }

                    #endregion

                    Logger.Trace("Left main read loop for some reason. Aborting: {0}, Shutting Down: {1}", _aborting,
                        _shutdown);
                }
                else
                {
                    Logger.Error("Failed to connect for some reason.");
                    EnqueueMessage(new ConnectionFailedMessage { FailedPipe = _targetPipe });
                }

                // If we are not aborting, we have to wait a bit before trying to connect again
                if (_aborting || _shutdown) continue;
                // We have disconnected for some reason, either a failed pipe or a bad reading,
                // so we are going to wait a bit before doing it again
                long sleep = _delay.NextDelay();

                Logger.Trace("Waiting {0}ms before attempting to connect again", sleep);
                Thread.Sleep(_delay.NextDelay());
            }
            // catch(InvalidPipeException e)
            // {
            //    Logger.Error("Invalid Pipe Exception: {0}", e.Message);
            // }
            catch (Exception e)
            {
                Logger.Error("Unhandled Exception: {0}", e.GetType().FullName);
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
            finally
            {
                // Disconnect from the pipe because something bad has happened. An exception has been thrown or the main read loop has terminated.
                if (_namedPipe.IsConnected)
                {
                    // Terminate the pipe
                    Logger.Trace("Closing the named pipe.");
                    _namedPipe.Close();
                }

                // Update our state
                SetConnectionState(RpcState.Disconnected);
            }

        // We have disconnected, so dispose of the thread and the pipe.
        Logger.Trace("Left Main Loop");
        _namedPipe?.Dispose();

        Logger.Info("Thread Terminated, no longer performing RPC connection.");
    }

    #region Writting

    private void ProcessCommandQueue()
    {
        // Logger.Info("Checking command queue");

        // We are not ready yet, don't even try
        if (State != RpcState.Connected)
            return;

        // We are aborting, so we will just log a warning, so we know this is probably only going to send the CLOSE
        if (_aborting)
            Logger.Warning("We have been told to write a queue but we have also been aborted.");

        // Prepare some variables we will clone into with locks
        var needsWriting = true;

        // Continue looping until we don't need anymore messages
        while (needsWriting && _namedPipe.IsConnected)
        {
            ICommand item;
            lock (_lockTxQueue)
            {
                // Pull the value and update our writing needs
                // If we have nothing to write, exit the loop
                needsWriting = _txQueue.Count > 0;
                if (!needsWriting) break;

                // Peek at the item
                item = _txQueue.Peek();
            }

            // Break out of the loop as soon as we send this item
            if (_shutdown || (!_aborting && LockStep))
                needsWriting = false;

            // Prepare the payload
            var payload = item.PreparePayload(GetNextNonce());
            Logger.Trace("Attempting to send payload: {0}", payload.Command);

            // Prepare the frame
            var frame = new PipeFrame();
            if (item is CloseCommand)
            {
                // We have been sent a close frame. We better just send a handwave
                // Send it off to the server
                SendHandwave();

                // Queue the item
                Logger.Trace("Handwave sent, ending queue processing.");
                lock (_lockTxQueue)
                {
                    _txQueue.Dequeue();
                }

                // Stop sending any more messages
                return;
            }

            if (_aborting)
            {
                // We are aborting, so just dequeue the message and don't bother sending it
                Logger.Warning("- skipping frame because of abort.");
                lock (_lockTxQueue)
                {
                    _txQueue.Dequeue();
                }
            }
            else
            {
                // Prepare the frame
                frame.SetObject(Opcode.Frame, payload);

                // Write it and if it wrote perfectly fine, we will dequeue it
                Logger.Trace("Sending payload: {0}", payload.Command);
                if (_namedPipe.WriteFrame(frame))
                {
                    // We sent it, so now dequeue it
                    Logger.Trace("Sent Successfully.");
                    lock (_lockTxQueue)
                    {
                        _txQueue.Dequeue();
                    }
                }
                else
                {
                    // Something went wrong, so just give up and wait for the next time around.
                    Logger.Warning("Something went wrong during writing!");
                    return;
                }
            }
        }
    }

    #endregion

    #region States

    /// <summary>
    ///     The current state of the RPC connection
    /// </summary>
    private RpcState State
    {
        get
        {
            lock (_lockStates)
            {
                return _state;
            }
        }
    }

    private RpcState _state;
    private readonly object _lockStates = new();

    /// <summary>
    ///     The configuration received by the Ready
    /// </summary>
    public Configuration Configuration
    {
        get
        {
            Configuration tmp;
            lock (_lockConfig)
            {
                tmp = _configuration;
            }

            return tmp;
        }
    }

    private Configuration _configuration;
    private readonly object _lockConfig = new();

    private volatile bool _aborting;
    private volatile bool _shutdown;

    /// <summary>
    ///     Indicates if the RPC connection is still running in the background
    /// </summary>
    public bool IsRunning => _thread != null;

    /// <summary>
    ///     Forces the <see cref="Close" /> to call <see cref="Shutdown" /> instead, safely saying goodbye to Discord.
    ///     <para>
    ///         This option helps prevents ghosting in applications where the Process ID is a host and the game is executed
    ///         within the host (ie: the Unity3D editor). This will tell Discord that we have no presence, and we are closing
    ///         the connection manually, instead of waiting for the process to terminate.
    ///     </para>
    /// </summary>
    public bool ShutdownOnly { get; set; }

    #endregion

    #region Privates

    private readonly string _applicationID; // ID of the Discord APP
    private readonly int _processID; // ID of the process to track

    private long _nonce; // Current command index

    private Thread _thread; // The current thread
    private readonly INamedPipeClient _namedPipe;

    private readonly int _targetPipe; // The pipe to target. Leave as -1 for any available pipe.

    private readonly object _lockTxQueue = new(); // Lock for the transmit-queue
    private readonly uint _maxTxQueueSize; // The max size of the transmit-queue
    private readonly Queue<ICommand> _txQueue; // The transmit-queue

    private readonly object _lockRxQueue = new(); // Lock for the receive-queue
    private readonly uint _maxRxQueueSize; // The max size of the receive-queue
    private readonly Queue<Message.Message> _rxQueue; // The receive-queue

    private readonly AutoResetEvent _queueUpdatedEvent = new(false);
    private readonly BackoffDelay _delay; // The backoff delay before reconnecting.

    #endregion

    #region Queues

    /// <summary>
    ///     Enqueues a command
    /// </summary>
    /// <param name="command">The command to enqueue</param>
    internal void EnqueueCommand(ICommand command)
    {
        Logger.Trace("Enqueue Command: {0}", command.GetType().FullName);

        // We cannot add anything else if we are aborting or shutting down.
        if (_aborting || _shutdown) return;

        // Enqueue the set presence argument
        lock (_lockTxQueue)
        {
            // If we are too big drop the last element
            if (_txQueue.Count == _maxTxQueueSize)
            {
                Logger.Error(
                    "Too many enqueued commands, dropping oldest one. Maybe you are pushing new presences to fast?");
                _txQueue.Dequeue();
            }

            // Enqueue the message
            _txQueue.Enqueue(command);
        }
    }

    /// <summary>
    ///     Adds a message to the message queue. Does not copy the message, so be sure to copy it yourself or dereference it.
    /// </summary>
    /// <param name="message">The message to add</param>
    private void EnqueueMessage(Message.Message message)
    {
        // Invoke the message
        try
        {
            if (OnRpcMessage != null)
                OnRpcMessage.Invoke(this, message);
        }
        catch (Exception e)
        {
            Logger.Error("Unhandled Exception while processing event: {0}", e.GetType().FullName);
            Logger.Error(e.Message);
            Logger.Error(e.StackTrace);
        }

        // Small queue sizes should just ignore messages
        if (_maxRxQueueSize <= 0)
        {
            Logger.Trace("Enqueued Message, but queue size is 0.");
            return;
        }

        // Large queue sizes should keep the queue in check
        Logger.Trace("Enqueue Message: {0}", message.Type);
        lock (_lockRxQueue)
        {
            // If we are too big drop the last element
            if (_rxQueue.Count == _maxRxQueueSize)
            {
                Logger.Warning("Too many enqueued messages, dropping oldest one.");
                _rxQueue.Dequeue();
            }

            // Enqueue the message
            _rxQueue.Enqueue(message);
        }
    }

    /// <summary>
    ///     Dequeues a single message from the event stack. Returns null if none are available.
    /// </summary>
    /// <returns></returns>
    internal Message.Message DequeueMessage()
    {
        // Logger.Trace("Deque Message");
        lock (_lockRxQueue)
        {
            // We have nothing, so just return null.
            // Or get the value and remove it from the list at the same time
            return _rxQueue.Count == 0 ? null : _rxQueue.Dequeue();
        }
    }

    /// <summary>
    ///     Dequeues all messages from the event stack.
    /// </summary>
    /// <returns></returns>
    internal Message.Message[] DequeueMessages()
    {
        // Logger.Trace("Deque Multiple Messages");
        lock (_lockRxQueue)
        {
            // Copy the messages into an array
            var messages = _rxQueue.ToArray();

            // Clear the entire queue
            _rxQueue.Clear();

            // return the array
            return messages;
        }
    }

    #endregion

    #region Reading

    /// <summary>Handles the response from the pipe and calls appropriate events and changes states.</summary>
    /// <param name="response">The response received by the server.</param>
    private void ProcessFrame(EventPayload response)
    {
        Logger.Info("Handling Response. Cmd: {0}, Event: {1}", response.Command, response.Event);

        // Check if it is an error
        if (response.Event is ServerEvent.ERROR)
        {
            // We have an error
            Logger.Error("Error received from the RPC");

            // Create the event object and push it to the queue
            var err = response.GetObject<ErrorMessage>();
            Logger.Error("Server responded with an error message: ({0}) {1}", err.Code.ToString(), err.Message);

            // Enqueue the message and then end
            EnqueueMessage(err);
            return;
        }

        // Check if it's a handshake
        if (State == RpcState.Connecting)
            if (response.Command == Command.DISPATCH && response.Event is ServerEvent.READY)
            {
                Logger.Info("Connection established with the RPC");
                SetConnectionState(RpcState.Connected);
                _delay.Reset();

                // Prepare the object
                var ready = response.GetObject<ReadyMessage>();
                lock (_lockConfig)
                {
                    _configuration = ready.Configuration;
                    ready.User.SetConfiguration(_configuration);
                }

                // Enqueue the message
                EnqueueMessage(ready);
                return;
            }

        if (State == RpcState.Connected)
        {
            VoiceSettings voiceSettings;
            switch (response.Command)
            {
                // We were sent a dispatch, better process it
                case Command.DISPATCH:
                    ProcessDispatch(response);
                    break;

                case Command.AUTHORIZE:
                    var oAuth2Authorization = response.GetObject<AuthorizeResponse>();
                    EnqueueMessage(new AuthorizeMessage(oAuth2Authorization));
                    break;

                case Command.AUTHENTICATE:
                    var oAuth2Authentication = response.GetObject<AuthenticateResponse>();
                    EnqueueMessage(new AuthenticateMessage(oAuth2Authentication));
                    break;

                case Command.SET_VOICE_SETTINGS:
                    voiceSettings = response.GetObject<VoiceSettings>();
                    EnqueueMessage(new VoiceSettingsMessage(voiceSettings));
                    break;

                // We were sent an Activity Update, better enqueue it
                case Command.SET_ACTIVITY:
                    if (response.Data == null)
                    {
                        EnqueueMessage(new PresenceMessage());
                    }
                    else
                    {
                        var rp = response.GetObject<RichPresenceResponse>();
                        EnqueueMessage(new PresenceMessage(rp));
                    }

                    break;

                case Command.UNSUBSCRIBE:
                case Command.SUBSCRIBE:

                    // Go through the data, looking for the evt property, casting it to a server event
                    var serverEvent = response.GetObject<EventPayload>().Event;
                    if (serverEvent != null)
                    {
                        var evt = serverEvent.Value;

                        // Enqueue the appropriate message.
                        if (response.Command == Command.SUBSCRIBE)
                            EnqueueMessage(new SubscribeMessage(evt));
                        else
                            EnqueueMessage(new UnsubscribeMessage(evt));
                    }

                    break;

                case Command.SEND_ACTIVITY_JOIN_INVITE:
                    Logger.Trace("Got invite response ack.");
                    break;

                case Command.CLOSE_ACTIVITY_JOIN_REQUEST:
                    Logger.Trace("Got invite response reject ack.");
                    break;

                // we have no idea what we were sent
                default:
                    Logger.Error("Unknown frame was received! {0}", response.Command);
                    return;
            }

            return;
        }

        Logger.Trace("Received a frame while we are disconnected. Ignoring. Cmd: {0}, Event: {1}", response.Command,
            response.Event);
    }

    private void ProcessDispatch(EventPayload response)
    {
        if (response.Command != Command.DISPATCH) return;
        if (!response.Event.HasValue) return;

        switch (response.Event.Value)
        {
            // We are to join the server
            case ServerEvent.ACTIVITY_SPECTATE:
                var spectate = response.GetObject<SpectateMessage>();
                EnqueueMessage(spectate);
                break;

            case ServerEvent.ACTIVITY_JOIN:
                var join = response.GetObject<JoinMessage>();
                EnqueueMessage(join);
                break;

            case ServerEvent.ACTIVITY_JOIN_REQUEST:
                var request = response.GetObject<JoinRequestMessage>();
                EnqueueMessage(request);
                break;

            // Unknown dispatch event received. We should just ignore it.
            case ServerEvent.READY:
            case ServerEvent.ERROR:
            default:
                Logger.Warning("Ignoring {0}", response.Event.Value);
                break;
        }
    }

    #endregion

    #region Connection

    /// <summary>
    ///     Establishes the handshake with the server.
    /// </summary>
    /// <returns></returns>
    private void EstablishHandshake()
    {
        Logger.Trace("Attempting to establish a handshake...");

        // We are establishing a lock and not releasing it until we sent the handshake message.
        // We need to set the key, and it would not be nice if someone did things between us setting the key.

        // Check its state
        if (State != RpcState.Disconnected)
        {
            Logger.Error("State must be disconnected in order to start a handshake!");
            return;
        }

        // Send it off to the server
        Logger.Trace("Sending Handshake...");
        if (!_namedPipe.WriteFrame(new PipeFrame(Opcode.Handshake,
                new Handshake { Version = Version, ClientID = _applicationID })))
        {
            Logger.Error("Failed to write a handshake.");
            return;
        }

        // This has to be done outside the lock
        SetConnectionState(RpcState.Connecting);
    }

    /// <summary>
    ///     Establishes a farewell with the server by sending a handwave.
    /// </summary>
    private void SendHandwave()
    {
        Logger.Info("Attempting to wave goodbye...");

        // Check its state
        if (State == RpcState.Disconnected)
        {
            Logger.Error("State must NOT be disconnected in order to send a handwave!");
            return;
        }

        // Send the handwave
        if (_namedPipe.WriteFrame(new PipeFrame(Opcode.Close,
                new Handshake { Version = Version, ClientID = _applicationID }))) return;
        Logger.Error("failed to write a handwave.");
    }

    /// <summary>
    ///     Attempts to connect to the pipe. Returns true on success
    /// </summary>
    /// <returns></returns>
    public bool AttemptConnection()
    {
        Logger.Info("Attempting a new connection");

        // The thread mustn't exist already
        if (_thread != null)
        {
            Logger.Error("Cannot attempt a new connection as the previous connection thread is not null!");
            return false;
        }

        // We have to be in the disconnected state
        if (State != RpcState.Disconnected)
        {
            Logger.Warning("Cannot attempt a new connection as the previous connection hasn't changed state yet.");
            return false;
        }

        if (_aborting)
        {
            Logger.Error("Cannot attempt a new connection while aborting!");
            return false;
        }

        // Start the thread up
        _thread = new Thread(MainLoop)
        {
            Name = "Discord IPC Thread",
            IsBackground = true
        };
        _thread.Start();

        return true;
    }

    /// <summary>
    ///     Sets the current state of the pipe, locking the l_states object for thread safety.
    /// </summary>
    /// <param name="state">The state to set it too.</param>
    private void SetConnectionState(RpcState state)
    {
        Logger.Trace("Setting the connection state to {0}", state.ToString().ToSnakeCase().ToUpperInvariant());
        lock (_lockStates)
        {
            _state = state;
        }
    }

    /// <summary>
    ///     Closes the connection and disposes of resources. This will not force termination, but instead allow Discord
    ///     disconnect us after we say goodbye.
    ///     <para>
    ///         This option helps prevents ghosting in applications where the Process ID is a host and the game is executed
    ///         within the host (ie: the Unity3D editor). This will tell Discord that we have no presence, and we are closing
    ///         the connection manually, instead of waiting for the process to terminate.
    ///     </para>
    /// </summary>
    private void Shutdown()
    {
        // Enable the flag
        Logger.Trace("Initiated shutdown procedure");
        _shutdown = true;

        // Clear the commands and enqueue the close
        lock (_lockTxQueue)
        {
            _txQueue.Clear();
            if (ClearOnShutdown) _txQueue.Enqueue(new PresenceCommand { PID = _processID, Presence = null });
            _txQueue.Enqueue(new CloseCommand());
        }

        // Trigger the event
        _queueUpdatedEvent.Set();
    }

    /// <summary>
    ///     Closes the connection and disposes of resources.
    /// </summary>
    public void Close()
    {
        if (_thread == null)
        {
            Logger.Error("Cannot close as it is not available!");
            return;
        }

        if (_aborting)
        {
            Logger.Error("Cannot abort as it has already been aborted");
            return;
        }

        // Set the abort state
        if (ShutdownOnly)
        {
            Shutdown();
            return;
        }

        // Terminate
        Logger.Trace("Updating Abort State...");
        _aborting = true;
        _queueUpdatedEvent.Set();
    }

    /// <summary>
    ///     Closes the connection and disposes resources. Identical to <see cref="Close" /> but ignores the "ShutdownOnly"
    ///     value.
    /// </summary>
    public void Dispose()
    {
        ShutdownOnly = false;
        Close();
    }

    #endregion
}

/// <summary>
///     State of the RPC connection
/// </summary>
internal enum RpcState
{
    /// <summary>
    ///     Disconnected from the discord client
    /// </summary>
    Disconnected,

    /// <summary>
    ///     Connecting to the discord client. The handshake has been sent, and we are awaiting the ready event
    /// </summary>
    Connecting,

    /// <summary>
    ///     We are connect to the client and can send and receive messages.
    /// </summary>
    Connected
}