using System.Net.Sockets;
using System.Text;
using System.Net;

namespace SocketTest {
	class Server {
		public const string EOF = "<EOF>";

		public static async Task Start() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = host.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);


			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try {
				Console.WriteLine("Server socket starting!");
				listener.Bind(localEndPoint);
				listener.Listen(10);

				while (true) {
					Socket handler = await listener.AcceptAsync();
					while (true) {
						Console.WriteLine("Server socket accepted connection!");
						string resp = await Receive(handler);
						if (resp.Contains(EOF)) {
							Console.WriteLine("EOF Received");
							Send(handler, resp);
						}
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
		}


		public static async Task<string> Receive(Socket handler) {
			byte[] buffer = new byte[1024];
			int rec = await handler.ReceiveAsync(buffer, 0);
			String response = Encoding.UTF8.GetString(buffer, 0, rec);
			if (response.Contains(EOF)) {
				Console.WriteLine($"Socket server receieved: {response.Replace(EOF, "")}");
				return response;
			}
			return "";
		}


		public static async void Send(Socket handler, String content) {
			byte[] data = Encoding.UTF8.GetBytes(content);
			await handler.SendAsync(data, 0);
			Console.WriteLine($"Server Sent: {data}");
		}

	}
}
