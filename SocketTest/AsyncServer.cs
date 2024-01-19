using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketTest {
	class AsyncServer {
		public static List<ServerState> states = new List<ServerState>();

		public class ServerState {
			public Socket handler;
			public byte[] buffer = new byte[1024];
			public string content = "";
			public string username = "";

			public ServerState(Socket handler) {
				this.handler = handler;
			}
		}


		public static ManualResetEvent threadEvent = new ManualResetEvent(false);


		public static void Start() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = host.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);

			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try {
				listener.Bind(localEndPoint);
				listener.Listen(10);

				while (true) {
					Console.WriteLine("Current thread: " + Thread.CurrentThread.ManagedThreadId);
					threadEvent.Reset(); // Resets the value of the event to false.
					Console.WriteLine("Waiting for connection...");
					listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
					threadEvent.WaitOne(); // Blocks the main thread until signal received.
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}


		public static void AcceptCallback(IAsyncResult asyncResult) {
			threadEvent.Set();
			Console.WriteLine("Current thread accepted: " + Thread.CurrentThread.ManagedThreadId);
			Socket listener = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(
					nameof(asyncResult.AsyncState),
					"Listener cannot be null."
					));

			Socket handler = listener.EndAccept(asyncResult);
			ServerState state = new ServerState(handler);
			if (!states.Contains(state))
				states.Add(state);

			Send(handler, "BOSHboshTELLmeUSERNAME");

			handler.BeginReceive(state.buffer, 0, 1024, 0, new AsyncCallback(ReadCallback), state);
		}


		public static void ReadCallback(IAsyncResult asyncResult) {
			ServerState state = (ServerState) (asyncResult.AsyncState ?? throw new ArgumentNullException(
						nameof(asyncResult.AsyncState),
						"Server state cannot be null."
						));

			Socket handler = state.handler;
			int bytesRead = handler.EndReceive(asyncResult);

			if (bytesRead < 1) {
				return;
			}

			state.content = (Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
			string newContent = state.content;
			string username = state.username;
			Console.WriteLine($"REC: {newContent}");

			if (newContent.Contains("<USERNAME>")) {
				state.username = newContent.Replace("<USERNAME>", "");
				username = state.username;
				Console.WriteLine($"Username: {username}");
				states.ForEach((state) => Send(state.handler, username + " has joined the chat."));
			}

			if (newContent.Contains("<EOF>")) {
				Console.WriteLine($"Read {newContent.Length} bytes \nContent: {newContent}");
				Console.WriteLine($"States Count: {states.Count}");
				states.ForEach((state) => Send(state.handler, username + ": " + newContent.Replace("<EOF>", "")));
			}
			handler.BeginReceive(state.buffer, 0, 1024, 0, new AsyncCallback(ReadCallback), state);
		}


		public static void Send(Socket handler, String content) {
			byte[] data = Encoding.UTF8.GetBytes(content);

			handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), handler);
		}

		public static void SendCallback(IAsyncResult asyncResult) {
			try {
				Socket handler = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(
						nameof(asyncResult.AsyncState),
						"handler cannot be null."
						));

				int sent = handler.EndSend(asyncResult);
				Console.WriteLine($"Sent {sent} bytes to client");

				/* handler.Shutdown(SocketShutdown.Both); */
				/* handler.Close(); */
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}
	}
}
