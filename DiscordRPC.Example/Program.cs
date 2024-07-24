using DiscordRPC.Message;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiscordRPC.Helper;
using DiscordRPC.RPC.Commands;

namespace DiscordRPC.Example
{
    static class Program
    {
        /// <summary>
        /// The level of logging to use.
        /// </summary>
        private static readonly Logging.LogLevel LogLevel = Logging.LogLevel.Trace;

        /// <summary>
        /// The pipe to connect too.
        /// </summary>
        private static int _discordPipe = -1;

        /// <summary>
        /// The current presence to send to discord.
        /// </summary>
        private static readonly RichPresence Presence = new RichPresence()
        {
            Details = "Example Project 🎁",
            State = "csharp example",
            Assets = new Assets()
            {
                LargeImageKey = "image_large",
                LargeImageText = "Lachee's Discord IPC Library",
                SmallImageKey = "image_small"
            }
        };

        /// <summary>
        /// The discord client
        /// </summary>
        private static DiscordRpcClient _client;

        /// <summary>
        /// Is the main loop currently running?
        /// </summary>
        private static bool _isRunning = true;

        /// <summary>
        /// The string builder for the command
        /// </summary>
        private static readonly StringBuilder Word = new StringBuilder();

        // Main Loop
        static void Main(string[] args)
        {
            // Reads the arguments for the pipe
            for (var i = 0; i < args.Length; i++)
            {
                _discordPipe = args[i] switch
                {
                    "-pipe" => int.Parse(args[++i]),
                    _ => _discordPipe
                };
            }

            // Setting a random details to test the update rate of the presence
            Task.Run(BasicExampleAsync).Wait();
            // ReadyTaskExample();
            // FullClientExample();
            // Issue104();
            // IssueMultipleSets();
            // IssueJoinLogic();

            Console.WriteLine("Press any key to terminate");
            Console.ReadKey();
        }
        
        private static TaskCompletionSource<bool> _authorizeCompletionSource;
        private static async Task BasicExampleAsync()
        {
            DiscordOAuth.SetClientData("1262811483302268988", "0U3f_GHE1shG-KQ-H41cr7MDoPqKlE8c");
            
            // == Create the client
            _client = new DiscordRpcClient("1262811483302268988", pipe: _discordPipe)
            {
                Logger = new Logging.ConsoleLogger(LogLevel, true)
            };
            
            _client.OnAuthorize += async (_, msg) => await OnAuthorize(msg);
            
            _client.OnAuthenticate += (_, msg) =>
            {
                Console.WriteLine("Authenticated with Discord and Expires at {0}", msg.Expires);
            };
            
            _client.OnVoiceSettings += (_, msg) =>
            {
                Console.WriteLine("Message arrived and mute is on: {0}", msg.VoiceSettings.Mute);
            };

            // == Subscribe to some events
            _client.OnReady += (_, msg) =>
            {
                // Create some events so we know things are happening
                Console.WriteLine("Connected to discord with user {0}", msg.User.Username);
            };

            _client.OnPresenceUpdate += (_, _) =>
            {
                // The presence has updated
                Console.WriteLine("Presence has been updated! ");
            };

            // == Initialize
            _client.Initialize();
            
            // == Do the rest of your program.
            // Simulated by a Console.ReadKey
            // etc...
            Console.ReadKey();

            if (await DiscordOAuth.TryRefreshToken() == false)
            {
                Console.WriteLine("BasicExampleAsync: Token couldn't be refreshed, trying to Authorize and get a new one.");
                // Initialize the TaskCompletionSource
                _authorizeCompletionSource = new TaskCompletionSource<bool>();
                _client.Authorize("1262811483302268988", ["rpc", "identify"]);
                // Wait for the authorization process to complete
                await _authorizeCompletionSource.Task;
            }

            var oAuthData = DiscordOAuth.LoadOAuthData();
            if (oAuthData.IsSome)
            {
                Console.WriteLine("Access Token: " + oAuthData.Value["access_token"]);
                _client.Authenticate(oAuthData.Value["access_token"].ToString());
            } else
            {
                Console.WriteLine("No access token found.");
            }
            
            Console.ReadKey();
            
            _client.GetVoiceSettings();
            
            _client.SetVoiceSettings(new VoiceSettings {Mute = true});
            
            
            // == Set the presence
            // client.SetPresence(new RichPresence()
            // {
            //     Details = "A Basic Example",
            //     State = "In Game",
            //     Timestamps = Timestamps.FromTimeSpan(10),
            //     Buttons = new Button[]
            //     {
            //         new Button() { Label = "Fish", Url = "https://lachee.dev/" }
            //     }
            // });

            // == Do the rest of your program.
            // Simulated by a Console.ReadKey
            // etc...
            Console.ReadKey();

            // == At the very end we need to dispose of it
            _client.Dispose();
        }
        
        private static void FullClientExample()
        {
            // Create a new DiscordRpcClient. We are filling some of the defaults as examples.
            using (_client = new DiscordRpcClient("1262811483302268988",          // The client ID of your Discord Application
                    pipe: _discordPipe,                                          // The pipe number we can locate discord on. If -1, then we will scan.
                    logger: new Logging.ConsoleLogger(LogLevel, true),          // The logger to get information back from the client.
                    autoEvents: true,                                           // Should the events be automatically called?
                    client: new IO.ManagedNamedPipeClient()                     // The pipe client to use. Required in mono to be changed.
                ))
            {
                // If you are going to make use of the Join / Spectate buttons, you are required to register the URI Scheme with the client.
                _client.RegisterUriScheme();

                // Set the logger. This way we can see the output of the client.
                // We can set it this way, but doing it directly in the constructor allows for the Register Uri Scheme to be logged too.
                // System.IO.File.WriteAllBytes("discord-rpc.log", new byte[0]);
                // client.Logger = new Logging.FileLogger("discord-rpc.log", DiscordLogLevel);

                // Register to the events we care about. We are registering to everyone just to show off the events
                
                _client.OnAuthorize += async (_, msg) => await OnAuthorize(msg);

                _client.OnReady += OnReady;                                      // Called when the client is ready to send presences
                _client.OnClose += OnClose;                                      // Called when connection to discord is lost
                _client.OnError += OnError;                                      // Called when discord has an error

                _client.OnConnectionEstablished += OnConnectionEstablished;      // Called when a pipe connection is made, but not ready
                _client.OnConnectionFailed += OnConnectionFailed;                // Called when a pipe connection failed.

                _client.OnPresenceUpdate += OnPresenceUpdate;                    // Called when the presence is updated

                _client.OnSubscribe += OnSubscribe;                              // Called when an event is subscribed too
                _client.OnUnsubscribe += OnUnsubscribe;                          // Called when an event is unsubscribed from.

                _client.OnJoin += OnJoin;                                        // Called when the client wishes to join someone else. Requires RegisterUriScheme to be called.
                _client.OnSpectate += OnSpectate;                                // Called when the client wishes to spectate someone else. Requires RegisterUriScheme to be called.
                _client.OnJoinRequested += OnJoinRequested;                      // Called when someone else has requested to join this client.

                // Before we send an initial presence, we will generate a random "game ID" for this example.
                // For a real game, this "game ID" can be a unique ID that your Match Maker / Master Server generates.
                // This is used for the Join / Spectate feature. This can be ignored if you do not plan to implement that feature.
                Presence.Secrets = new Secrets()
                {
                    // These secrets should contain enough data for external clients to be able to know which
                    // game to connect too. A simple approach would be just to use IP address, but this is highly discouraged
                    // and can leave your players vulnerable!
                    JoinSecret = "join_myuniquegameid",
                    SpectateSecret = "spectate_myuniquegameid"
                };

                // We also need to generate an initial party. This is because Join requires the party to be created too.
                // If no party is set, the join feature will not work and may cause errors within the discord client itself.
                Presence.Party = new Party()
                {
                    ID = Secrets.CreateFriendlySecret(new Random()),
                    CurrentSize = 1,
                    Max = 4,
                    Privacy = Party.PrivacySetting.Public
                };

                // Give the game some time, so we have a nice countdown
                Presence.Timestamps = new Timestamps()
                {
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow + TimeSpan.FromSeconds(15)
                };

                // Subscribe to the join / spectate feature.
                // These require the RegisterURI to be true.
                _client.SetSubscription(EventType.Join | EventType.Spectate | EventType.JoinRequest);        // This will alert us if discord wants to join a game

                // Set some new presence to tell Discord we are in a game.
                // If the connection is not yet available, this will be queued until a Ready event is called,
                // then it will be sent. All messages are queued until Discord is ready to receive them.
                _client.SetPresence(Presence);

                // Initialize the connection. This must be called ONLY once.
                // It must be called before any updates are sent or received from the discord client.
                _client.Initialize();

                // Start our main loop. In a normal game you probably don't have to do this step.
                // Just make sure you call .Invoke() or some other dequeuing event to receive your events.
                MainLoop();
            }
        }
        static void MainLoop()
        {
            /*
             * Enter an infinite loop, polling the Discord Client for events.
             * In game terminology, this will be equivalent to our main game loop.
             * If you were making a GUI application without an infinite loop, you could implement
             * this with timers.
            */
            _isRunning = true;
            while (_client != null && _isRunning)
            {
                // We will invoke the client events.
                // In a game situation, you would do this in the Update.
                // Not required if AutoEvents is enabled.
                // if (client != null && !client.AutoEvents)
                //    client.Invoke();

                // Try to read any keys if available
                if (Console.KeyAvailable)
                    ProcessKey();

                // This can be what ever value you want, as long as it is faster than 30 seconds.
                // Console.Write("+");
                Thread.Sleep(25);

                _client.SetPresence(Presence);
            }

            Console.WriteLine("Press any key to terminate");
            Console.ReadKey();
        }

        private static async void ReadyTaskExample()
        {
            var readyCompletionSource = new TaskCompletionSource<User>();

            // == Create the client
            // We are using the `using` keyword because we want to automatically call Dispose
            //  once we finish this method. We only care to get the user info.
            // If you want to update the presence, don't do this but rather make it a singleton
            //  that lives throughout the lifetime of your app.
            using var client = new DiscordRpcClient("1262811483302268988", pipe: _discordPipe);
            client.Logger = new Logging.ConsoleLogger(LogLevel, true);

            // == Sub to ready
            // We are going to listen to the On Ready. Once we have it, we will tell the completion
            //  source to continue with the result.
            client.OnReady += (_, msg) =>
            {
                readyCompletionSource.SetResult(msg.User);
            };

            // == Initialize
            client.Initialize();

            // == Wait for user
            var user = await readyCompletionSource.Task;
            Console.WriteLine("Connected to discord with user {0}: {1}", user.Username, user.Avatar);
        }

        #region Events

        #region State Events
        private static async Task OnAuthorize(AuthorizeMessage msg)
        {
            //This is called after the user has authorized the application.
            Console.WriteLine("On Authorize. Auth code: {0}", msg.Code);
            
            var oAuthCode = msg.Code;
                
            try
            {
                // Assuming ExchangeCodeAsync is properly awaited within
                var result = await DiscordOAuth.ExchangeCodeAsync(oAuthCode);
                Console.WriteLine("Access Token: " + result);
                _authorizeCompletionSource.SetResult(true);
            }
            catch (HttpRequestException e)
            {
                // Handle any exceptions
                Console.WriteLine("Request error: " + e.Message);
                _authorizeCompletionSource.SetResult(true);
            }
        }
        
        private static void OnReady(object sender, ReadyMessage args)
        {
            // This is called when we are all ready to start receiving and sending discord events.
            // It will give us some basic information about discord to use in the future.

            // DEBUG: Update the presence timestamp
            Presence.Timestamps = Timestamps.Now;

            // It can be a good idea to send an initial presence update on this event too, just to set up the initial game state.
            Console.WriteLine("On Ready. RPC Version: {0}", args.Version);

        }
        private static void OnClose(object sender, CloseMessage args)
        {
            // This is called when our client has closed. The client can no longer send or receive events after this message.
            // Connection will automatically try to re-establish and another OnReady will be called (unless it was disposed).
            Console.WriteLine("Lost Connection with client because of '{0}'", args.Reason);
        }
        private static void OnError(object sender, ErrorMessage args)
        {
            // Some error has occured from one of our messages. Could be a malformed presence for example.
            // Discord will give us one of these events and its upto us to handle it
            Console.WriteLine("Error occured within discord. ({1}) {0}", args.Message, args.Code);
        }
        #endregion

        #region Pipe Connection Events
        private static void OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {
            // This is called when a pipe connection is established. The connection is not ready yet, but we have at least found a valid pipe.
            Console.WriteLine("Pipe Connection Established. Valid on pipe #{0}", args.ConnectedPipe);
        }
        private static void OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            // This is called when the client fails to establish a connection to discord.
            // It can be assumed that Discord is unavailable on the supplied pipe.
            Console.WriteLine("Pipe Connection Failed. Could not connect to pipe #{0}", args.FailedPipe);
            _isRunning = false;
        }
        #endregion

        private static void OnPresenceUpdate(object sender, PresenceMessage args)
        {
            // This is called when the Rich Presence has been updated in the discord client.
            // Use this to keep track of the rich presence and validate that it has been sent correctly.
            Console.WriteLine("Rich Presence Updated. Playing {0}", args.Presence == null ? "Nothing (NULL)" : args.Presence.State);
        }

        #region Subscription Events
        private static void OnSubscribe(object sender, SubscribeMessage args)
        {
            // This is called when the subscription has been made successfully. It will return the event you subscribed too.
            Console.WriteLine("Subscribed: {0}", args.Event);
        }
        private static void OnUnsubscribe(object sender, UnsubscribeMessage args)
        {
            // This is called when the unsubscription has been made successfully. It will return the event you unsubscribed from.
            Console.WriteLine("Unsubscribed: {0}", args.Event);
        }
        #endregion

        #region Join / Spectate feature
        private static void OnJoin(object sender, JoinMessage args)
        {
            /*
             * This is called when the Discord Client wants to join an online game to play.
             * It can be triggered from an invite that your user has clicked on within discord or from an accepted invite.
             *
             * The secret should be some sort of encrypted data that will give your game the necessary information to connect.
             * For example, it could be the Game ID and the Game Password which will allow you to look up from the Master Server.
             * Please avoid using IP addresses within these fields, it's not secure and defeats the Discord security measures.
             *
             * This feature requires the RegisterURI to be true on the client.
            */
            Console.WriteLine("Joining Game '{0}'", args.Secret);
        }

        private static void OnSpectate(object sender, SpectateMessage args)
        {   /*
             * This is called when the Discord Client wants to join an online game to watch and spectate.
             * It can be triggered from an invite that your user has clicked on within discord.
             *
             * The secret should be some sort of encrypted data that will give your game the necessary information to connect.
             * For example, it could be the Game ID and the Game Password which will allow you to look up from the Master Server.
             * Please avoid using IP addresses within these fields, it's not secure and defeats the Discord security measures.
             *
             * This feature requires the RegisterURI to be true on the client.
            */
            Console.WriteLine("Spectating Game '{0}'", args.Secret);
        }

        private static void OnJoinRequested(object sender, JoinRequestMessage args)
        {
            /*
             * This is called when the Discord Client has received a request from another external Discord User to join your game.
             * You should trigger a UI prompt to your user sayings 'X wants to join your game' with a YES or NO button. You can also get
             *  other information about the user such as their avatar (which this library will provide a useful link) and their nickname to
             *  make it more personalised. You can combine this with more API if you wish. Check the Discord API documentation.
             *
             *  Once a user clicks on a response, call the `Respond` function, passing the message, to respond to the request.
             *  An example is provided below.
             *
             * This feature requires the RegisterURI to be true on the client.
            */

            // We have received a request, dump a bunch of information for the user
            Console.WriteLine("'{0}' has requested to join our game.", args.User);
            Console.WriteLine(" - User's Avatar: {0}", args.User.GetAvatarURL(User.AvatarFormat.GIF, User.AvatarSize.X2048));
            Console.WriteLine(" - User's Username: {0}", args.User.Username);
            Console.WriteLine(" - User's Snowflake: {0}", args.User.ID);
            Console.WriteLine();

            // Ask the user if they wish to accept the join request.
            Console.Write("Do you give this user permission to join? [Y / n]: ");
            bool accept = Console.ReadKey().Key == ConsoleKey.Y; Console.WriteLine();

            // Tell the client if we accept or not.
            DiscordRpcClient client = (DiscordRpcClient)sender;
            client.Respond(args, accept);

            // All done.
            Console.WriteLine(" - Sent a {0} invite to the client {1}", accept ? "ACCEPT" : "REJECT", args.User.Username);
        }
        #endregion

        #endregion

        private static int _cursorIndex;
        private static string _previousCommand = "";
        static void ProcessKey()
        {
            // Read they key
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    // Write the new line
                    Console.WriteLine();
                    _cursorIndex = 0;

                    // The enter key has been sent, so send the message
                    _previousCommand = Word.ToString();
                    ExecuteCommand(_previousCommand);

                    Word.Clear();
                    break;

                case ConsoleKey.Backspace:
                    Word.Remove(_cursorIndex - 1, 1);
                    Console.Write("\r                                         \r");
                    Console.Write(Word);
                    _cursorIndex--;
                    break;

                case ConsoleKey.Delete:
                    if (_cursorIndex < Word.Length)
                    {
                        Word.Remove(_cursorIndex, 1);
                        Console.Write("\r                                         \r");
                        Console.Write(Word);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    _cursorIndex--;
                    break;

                case ConsoleKey.RightArrow:
                    _cursorIndex++;
                    break;

                case ConsoleKey.UpArrow:
                    Word.Clear().Append(_previousCommand);
                    Console.Write("\r                                         \r");
                    Console.Write(Word);
                    break;

                default:
                    if (!Char.IsControl(key.KeyChar))
                    {
                        // Some other character key was sent
                        Console.Write(key.KeyChar);
                        Word.Insert(_cursorIndex, key.KeyChar);
                        Console.Write("\r                                         \r");
                        Console.Write(Word);
                        _cursorIndex++;
                    }
                    break;
            }

            if (_cursorIndex < 0) _cursorIndex = 0;
            if (_cursorIndex >= Console.BufferWidth) _cursorIndex = Console.BufferWidth - 1;
            Console.SetCursorPosition(_cursorIndex, Console.CursorTop);
        }

        static void ExecuteCommand(string word)
        {
            // Trim the extra spacing
            word = word.Trim();

            // Prepare the command and its body
            string command = word;
            string body = "";

            // Split the command and the values.
            int whitespaceIndex = word.IndexOf(' ');
            if (whitespaceIndex >= 0)
            {
                command = word.Substring(0, whitespaceIndex);
                if (whitespaceIndex < word.Length)
                    body = word.Substring(whitespaceIndex + 1);
            }

            // Parse the command
            switch (command.ToLowerInvariant())
            {
                case "close":
                    _client.Dispose();
                    break;

                #region State & Details
                case "state":
                    // presence.State = body;
                    Presence.State = body;
                    _client.SetPresence(Presence);
                    break;

                case "details":
                    Presence.Details = body;
                    _client.SetPresence(Presence);
                    break;
                #endregion

                #region Asset Examples
                case "large_key":
                    // If we do not have an asset object already, we must create it
                    if (!Presence.HasAssets())
                        Presence.Assets = new Assets();

                    // Set the key then send it away
                    Presence.Assets.LargeImageKey = body;
                    _client.SetPresence(Presence);
                    break;

                case "large_text":
                    // If we do not have an asset object already, we must create it
                    if (!Presence.HasAssets())
                        Presence.Assets = new Assets();

                    // Set the key then send it away
                    Presence.Assets.LargeImageText = body;
                    _client.SetPresence(Presence);
                    break;

                case "small_key":
                    // If we do not have an asset object already, we must create it
                    if (!Presence.HasAssets())
                        Presence.Assets = new Assets();

                    // Set the key then send it away
                    Presence.Assets.SmallImageKey = body;
                    _client.SetPresence(Presence);
                    break;

                case "small_text":
                    // If we do not have an asset object already, we must create it
                    if (!Presence.HasAssets())
                        Presence.Assets = new Assets();

                    // Set the key then send it away
                    Presence.Assets.SmallImageText = body;
                    _client.SetPresence(Presence);
                    break;
                #endregion

                case "help":
                    Console.WriteLine("Available Commands: state, details, large_key, large_text, small_key, small_text");
                    break;

                default:
                    Console.WriteLine("Unknown Command '{0}'. Try 'help' for a list of commands", command);
                    break;
            }
        }
    }
}