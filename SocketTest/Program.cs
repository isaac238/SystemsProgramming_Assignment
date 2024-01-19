namespace SocketTest {
	class Program {
		static void Main(string[] args) {
			Console.Clear();
			string input = "";

			if (args.Length > 0) {
				input = args[0];
			} else {
				Console.Write("Enter \n as for Async Server,\n ac for Async Client,\n : ");
				input = Console.ReadLine() ?? "";
			}
			
			if (input.ToLower() == "as")
				AsyncServer.Start();
			if (input.ToLower() == "ac")
				AsyncClient.Start();
		}
	}
}
