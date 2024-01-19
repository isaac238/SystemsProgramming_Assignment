using System.Net.Sockets;

namespace SystemsProgramming.Assigment {
	class Connection {
		public Socket socket;
		public byte[] buffer = new byte[1024];
		public string content = "";
		public User? user;

		public Connection(Socket socket) {
			this.socket = socket;
		}
	}
}
