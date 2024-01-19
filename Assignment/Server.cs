using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace SystemsProgramming.Assigment {
	class Server {
		private static List<Connection> _connections = new List<Connection>();
		public static List<Connection> connections { get => _connections; }

		public static IEnumerable<User> users { get => _connections.Where(connection => connection.user != null).Select(connection => connection.user!); }

		public static HashSet<Socket> removalQueue = new HashSet<Socket>();
		private static List<Command> _commands = new List<Command>(3) {
			new Command("users", "Lists all users connected to the server.", new UsersCommandImplementation(), new HashSet<String>() { "list", "u" }),
			new Command("exit", "Disconnects the client from the server.", new ExitCommandImplementation(), new HashSet<String>() { "quit", "q" }),
			new Command("help", "Lists all available commands.", new HelpCommandImplementation(), new HashSet<String>() { "h", "?" }, true),
		};

		public static List<Command> commands { get => _commands; }

		public static ManualResetEvent threadSignaler = new ManualResetEvent(false);
		public static ManualResetEvent removalQueueSignaler = new ManualResetEvent(false);

		private static void ProcessRemovalQueue() {
			foreach (Socket socket in removalQueue) {
				Console.WriteLine("Removing socket connection.");
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
				_connections = _connections.Where(connection => connection.socket != socket).ToList();
			}

			removalQueue.Clear();
			removalQueueSignaler.Set();
		}

		public static void Start() {
			IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			int port = UI.getPort();
			if (port == -1) return;
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try {
				listener.Bind(localEndPoint);
			} catch (SocketException e) {
				if (e.ErrorCode == 98) {
					AnsiConsole.MarkupLine($"[red]Port {port} is already in use.[/]");
					UI.waitForKey("[grey]Press any key to return to home...[/]");
					return;
				}
				AnsiConsole.WriteException(e);
			}

			listener.Listen(10);
			AnsiConsole.MarkupLine($"Server started on [red]{ipAddress}:{port}[/]");


			while (true) {
				threadSignaler.Reset(); // Resets signal to non-signaled state.
				listener.BeginAccept(new AsyncCallback(Connected), listener);
				threadSignaler.WaitOne(); // Blocks main thread (1) until Set called (Receives signal from other thread).
			}
		}

		// Handler for when a client has connected to the server.
		private static void Connected(IAsyncResult asyncResult) {
			threadSignaler.Set(); // Signals main thread (1) to continue (Runs on different thread) as connection established.
			Socket listener = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Listener cannot be null"));
			Socket handler = listener.EndAccept(asyncResult);
			Connection connection = new Connection(handler);

			removalQueueSignaler.Reset();
			_connections.Select(connection => connection).ToList().ForEach(connection => {
				Console.WriteLine("Sending alive check to: " + connection.user?.username);
				Send(connection.socket, "ConnectedCheck<|TYPE|>ALIVE_CHECK");
				Send(connection.socket, "ConnectedCheck<|TYPE|>ALIVE_CHECK");
			});
			ProcessRemovalQueue();
			removalQueueSignaler.WaitOne();

			if (!_connections.Contains(connection)) {
				_connections.Add(connection);
			}

			handler.BeginReceive(connection.buffer, 0, connection.buffer.Length, 0, new AsyncCallback(Received), connection);
		}

		// Handler for when data has been received from a client.
		private static void Received(IAsyncResult asyncResult) {
			Connection connection = (Connection) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Connection cannot be null"));
			Socket handler = connection.socket;

			try {
				int bytesReceived = handler.EndReceive(asyncResult);

				if (bytesReceived < 1) return;

				string receivedString = Encoding.UTF8.GetString(connection.buffer, 0, bytesReceived);
				String type = receivedString.Split("<|TYPE|>")[1];
				String data = receivedString.Split("<|TYPE|>")[0];

				JsonSerializerOptions options = new JsonSerializerOptions()
				{
					IncludeFields = true,
				};

				if (type == "USER") {
					User? user = JsonSerializer.Deserialize<User>(data, options);
					if (user != null) {
						Boolean userExists = users.Any(existingUser => existingUser.username == user.username);
						if (userExists) {
							Send(handler, $"{user.username} already exists!<|TYPE|>USER_EXISTS");
						}

						if (!userExists) {
							connection.user = user;
							Console.WriteLine($"Assigned connection to: {user?.username}.");
							_connections.ForEach(connection => {
								Send(connection.socket, new Message(new User("[bold]Server[/]"), $"{user?.username}, Joined the Room!").ToJSON());
							});
						}
					}
				}

				if (type == "MESSAGE") {
					Message? message = JsonSerializer.Deserialize<Message>(data, options);


					if (message == null) {
						Console.WriteLine("Failed to deserialize");
						handler.BeginReceive(connection.buffer, 0, connection.buffer.Length, 0, new AsyncCallback(Received), connection);
						return;
					}

					Boolean isCommand = message.content[0] == ':';

					if (isCommand) {
						Console.WriteLine("Command detected.");
						String commandText = message.content.Substring(1);
						Console.WriteLine($"Command text: {commandText}");
						try {
							Command commandToExecute = _commands.First(command => command.name == commandText || command.aliases.Contains(commandText));
							Message commandResult = commandToExecute.Execute(handler);
							Send(handler, commandResult.ToJSON());
						} catch (InvalidOperationException) {
							Send(handler, new Message(new User("[bold]Server[/]"), $"Command :{commandText} not found.").ToJSON());
						}
					}

					else {
						Console.WriteLine($"[{message.timestamp}] {type}: {message.sender.username} >> {message.content}");

						Console.WriteLine("Pushing message to all clients.");

						_connections.ForEach(connection => {
							Send(connection.socket, message?.ToJSON() ?? throw new ArgumentNullException(nameof(message), "Message cannot be null"));
						});
					}

				}

				removalQueueSignaler.Reset();
				ProcessRemovalQueue();
				removalQueueSignaler.WaitOne();

				if (_connections.Any((connection) => connection.socket == handler))
					handler.BeginReceive(connection.buffer, 0, connection.buffer.Length, 0, new AsyncCallback(Received), connection);

			} catch (SocketException e) {
				if (e.ErrorCode == 32) {
					Console.WriteLine("Broken pipe, maybe client disconnected?");
					removalQueue.Add(handler);
					return;
				}

				if (e.ErrorCode == 104) {
					Console.WriteLine("Connection reset by peer, maybe client disconnected?");
					removalQueue.Add(handler);
					return;
				}
				AnsiConsole.WriteException(e);
			}
		}

		// Sends string data to a client.
		private static void Send(Socket handler, String response) {
			try {
				byte[] data = Encoding.UTF8.GetBytes(response);
				handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(Sent), handler);
			} catch (SocketException e) {
				if (e.ErrorCode == 32) {
					Console.WriteLine("Broken pipe, maybe client disconnected?");
					removalQueue.Add(handler);
					return;
				}

				if (e.ErrorCode == 104) {
					Console.WriteLine("Connection reset by peer, maybe client disconnected?");
					removalQueue.Add(handler);
					return;
				}

				AnsiConsole.WriteException(e);
			}
		}

		private static void Sent(IAsyncResult asyncResult) {
			Socket handler = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Handler cannot be null"));
			int bytesSent = handler.EndSend(asyncResult);
		}
	}
}
