using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using Spectre.Console;

namespace SystemsProgramming.Assigment {
	class Client {
		private static ManualResetEvent connectEvent = new ManualResetEvent(false);
		public static ManualResetEvent sendEvent = new ManualResetEvent(false);
		private static ManualResetEvent receiveEvent = new ManualResetEvent(false);
		public static bool disconnected = false;

		private static List<Message> _messages = new List<Message>();
		public static List<Message> messages { get => _messages; }

		public static User? user;
		public static Socket? client = null;

		public static void Start() {
			// Reset variables in case of exit -> restart.
			disconnected = false;
			_messages = new List<Message>();
			user = null;
			Client.client = null;
			connectEvent.Reset();
			sendEvent.Reset();
			receiveEvent.Reset();

			// Get IP address and port from user.
			IPEndPoint ipEndPoint = UI.getIpEndpoint();
			Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			// Attempt to connect to server.
			client.BeginConnect(ipEndPoint, new AsyncCallback(Connected), client);
			connectEvent.WaitOne(); // Blocks the main thread until Set called (Connected).

			if (Client.client == null) return; // If client not set in Connected (failed) return out of client.

			user = UI.getUser();
			Send(client, user.ToJSON());
			Receive(client); // Start listening for any data sent by the server, recursive, listening never stops, thread unblocks whenever data is received.
			receiveEvent.WaitOne(); // Blocks the main thread until Set called (Data Received).
			while (!disconnected) {
				receiveEvent.Reset(); // Reset to non-signaled state so that it can be used again.
				UI.drawMessageScreen();
				receiveEvent.WaitOne(); // Blocks the main thread until Set called (Received Data).
			}

			UI.serverDisconnect(); // Loop ended, server disconnected, notify user.
			return;
		}

		public static void Connected(IAsyncResult asyncResult) {
			try {
				Socket client = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Client cannot be null"));
				client.EndConnect(asyncResult);
				Client.client = client;
				AnsiConsole.MarkupLine("[lime]Connected to server.[/]");
			} catch (SocketException e) {
				if (e.ErrorCode == 111) {
					AnsiConsole.Clear();
					AnsiConsole.MarkupLine("[red]Failed to connect[/]");
					UI.waitForKey("[grey]Press any key to go back...[/]");
					AnsiConsole.Clear();
				} else {
					AnsiConsole.WriteException(e);
				}
			}

			connectEvent.Set(); // Unblocks the main thread
		}

		public static void Receive(Socket client) {
			Connection connection = new Connection(client);
			client.BeginReceive(connection.buffer, 0, connection.buffer.Length, 0, new AsyncCallback(Received), connection);
		}

		public static void Received(IAsyncResult asyncResult) {
			Connection connection = (Connection) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Connection cannot be null"));
			Socket client = connection.socket;

			int bytesReceived = client.EndReceive(asyncResult);

			if (bytesReceived < 1) {
				disconnected = true;
			}

			if (disconnected) {
				receiveEvent.Set();
				return;
			}

			string receivedString = Encoding.UTF8.GetString(connection.buffer, 0, bytesReceived);
			String[] split = receivedString.Split("<|TYPE|>");
			String type = split.Last();
			String data = split.First();

			JsonSerializerOptions options = new JsonSerializerOptions()
			{
				IncludeFields = true,
			};

			if (type == "USER_EXISTS" && !disconnected) {
				AnsiConsole.MarkupLine($"[red]{data}[/]");
				UI.waitForKey("[grey]Press any key to go back...[/]");
				user = UI.getUser();
				Send(client, user.ToJSON());
				Receive(client); // Start listening for any data sent by the server, recursive, listening never stops, thread unblocks whenever data is received.
				receiveEvent.WaitOne(); // Blocks the main thread until Set called (Data Received).
			}

			if (type == "ALIVE_CHECK" && !disconnected) {
				Receive(client);
				/* receiveEvent.WaitOne(); */
			}

			if (type == "MESSAGE" && !disconnected) {
				try {
					Message? message = JsonSerializer.Deserialize<Message>(data, options); 

					if (message == null) { 
						_messages.Add(new Message(new User("[bold]Server[/]"), "Failed to deserialize"));
					}  

					if (message != null){ 
						_messages.Add(message);
					}

					Receive(client);
				} catch (Exception e) {
					AnsiConsole.WriteException(e);
				}
			}
			receiveEvent.Set();
		}

		// Sends string data to a client.
		public static void Send(Socket handler, String response) {
			byte[] data = Encoding.UTF8.GetBytes(response);
			handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(Sent), handler);
		}

		public static void Sent(IAsyncResult asyncResult) {
			Socket client = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(nameof(asyncResult), "Handler cannot be null"));
			int bytesSent = client.EndSend(asyncResult);
			sendEvent.Set();
		}

	}
}
