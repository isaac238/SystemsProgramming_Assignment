using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketTest {
	class AsyncClient {
		public static List<string> pastMessages = new List<string>();

		public class ClientState {
			public Socket client;
			public byte[] buffer = new byte[1024];
			public string content = "";
			public string username = "";

			public ClientState(Socket client) {
				this.client = client;
			}
		}


		private static ManualResetEvent connectEvent = new ManualResetEvent(false);
		private static ManualResetEvent sendEvent = new ManualResetEvent(false);
		private static ManualResetEvent receiveEvent = new ManualResetEvent(false);

		/* private static String response = String.Empty; */


		public static void Start() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = host.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);

			Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try {
				client.BeginConnect(localEndPoint, new AsyncCallback(ConnectCallback), client);
				connectEvent.WaitOne();
				Recieve(client);
				receiveEvent.WaitOne();

				while (true) {
					string content = Console.ReadLine() ?? "";
					if (content.ToLower() == "q") {
						Send(client, "<EOF>");
						sendEvent.WaitOne();
						break;
					} else {
						Send(client, content + "<EOF>");
						sendEvent.WaitOne();
					}

				}

				client.Shutdown(SocketShutdown.Both);
				client.Close();
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}


		public static void ConnectCallback(IAsyncResult asyncResult) {
			Socket client = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(
					nameof(asyncResult.AsyncState),
					"Client cannot be null."
					));

			client.EndConnect(asyncResult);

			Console.WriteLine($"Connected to: {client.RemoteEndPoint.ToString()}");
			receiveEvent.Set();
			connectEvent.Set();
		}


		private static void Recieve(Socket client) {
			ClientState state = new ClientState(client);
			client.BeginReceive(state.buffer, 0, 1024, 0, new AsyncCallback(RecieveCallback), state);
		}


		private static void RecieveCallback(IAsyncResult asyncResult) {
			ClientState state = (ClientState) (asyncResult.AsyncState ?? throw new ArgumentNullException(
						nameof(asyncResult.AsyncState),
						"Client state cannot be null."
						));

			Socket client = state.client;
			
			int bytes = client.EndReceive(asyncResult);

			if (bytes > 0) {
				state.content = (Encoding.UTF8.GetString(state.buffer, 0, bytes));
				if (state.content == "BOSHboshTELLmeUSERNAME") {
					Console.Write("Enter your username: ");
					state.username = Console.ReadLine() ?? "";
					Send(client, state.username + "<USERNAME>");
				} else {
					pastMessages.Add(state.content);
					Console.Clear();
					pastMessages.ForEach((message) => Console.WriteLine(message));
				}
				client.BeginReceive(state.buffer, 0, 1024, 0, new AsyncCallback(RecieveCallback), state);
			}
			receiveEvent.Set();
		}


		private static void Send(Socket client, String content) {
			byte[] data = Encoding.UTF8.GetBytes(content);

			client.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), client);
		}


		private static void SendCallback(IAsyncResult asyncResult) {
			Socket client = (Socket) (asyncResult.AsyncState ?? throw new ArgumentNullException(
					nameof(asyncResult.AsyncState),
					"Client cannot be null."
					));

			int sent = client.EndSend(asyncResult);
			sendEvent.Set();
		}

	}
}
