using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketTest {
	class Client {
		public static async Task Start() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = host.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);

			Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try {
				await client.ConnectAsync(localEndPoint);
				while (true) {
					Console.Write("Enter message: ");
					string message = Console.ReadLine() ?? "";
					Send(client, message);
					string resp = await Receive(client);
				}
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
		}

		public static async void Send(Socket client, string message) {
			if (!message.Contains("<EOF>"))
				message = $"{message}<EOF>";

			byte[] data = Encoding.UTF8.GetBytes(message);
			await client.SendAsync(data, 0);
			Console.WriteLine($"Client Sent: {message}");
		}


		public static async Task<string> Receive(Socket client) {
			byte[] buffer = new byte[1024];
			int rec = await client.ReceiveAsync(buffer, SocketFlags.None);
			String response = Encoding.UTF8.GetString(buffer, 0, rec);
			if (response.Contains("<EOF>")) {
				Console.WriteLine($"Socket client receieved: {response.Replace("<EOF>", "")}");
				return response;
			}
			return "";
		}
	}
}
