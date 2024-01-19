using System.Net;
using Spectre.Console;

namespace SystemsProgramming.Assigment {
	class UI {
		private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		private static CancellationToken cancellationToken = cancellationTokenSource.Token;
		private static ManualResetEvent uiBlocker = new ManualResetEvent(false);

		private static void drawMessageRows() {
			List<Panel> rows = Client.messages.Select((message) => message.ToPanel()).ToList();
			AnsiConsole.Write(new Rows(rows));
		}

		private static void drawInfoPanel() {
			Panel infoPanel = new Panel(
				new Rows(
					new Markup($"[grey]Username:[/] [red]{Client.user?.username}[/]"),
					new Markup($"[grey]Connected to:[/] [red]{Client.client?.RemoteEndPoint}[/]"),
					new Markup($"[grey]Open help menu by sending[/] [red bold]:h[/]")
				)
			).Header("Information")
			.Border(BoxBorder.Rounded)
			.Expand();

			AnsiConsole.Write(infoPanel);
		}

		public static void waitForKey(string message) {
			AnsiConsole.MarkupLine(message);
			AnsiConsole.Console.Input.ReadKey(false);
		}

		private static void refreshCancellationToken() {
			if (!cancellationTokenSource.IsCancellationRequested) {
				cancellationToken.Register(() => {
						AnsiConsole.MarkupLine("[red]Cancellation complete...[/]");
						cancellationTokenSource.Dispose();
						cancellationTokenSource = new CancellationTokenSource();
						cancellationToken = cancellationTokenSource.Token;
						AnsiConsole.MarkupLine("[red]Unblocking UI thread...[/]");
						uiBlocker.Set();
				});

				AnsiConsole.MarkupLine("[red]Requesting cancellation of token...[/]");
				cancellationTokenSource.Cancel();
				Thread.Sleep(10);
			}
		}

		public static async void drawMessageScreen() {
			String message = "";
			refreshCancellationToken();

			AnsiConsole.MarkupLine("[red]UI thread unblocked...[/]");
			while (message == "" && !cancellationToken.IsCancellationRequested) {
				AnsiConsole.Clear();
				Console.Clear();


				drawMessageRows();
				drawInfoPanel();

				try {
					message = await new TextPrompt<string>("[grey]Enter your message: [/]").ShowAsync(AnsiConsole.Console, cancellationToken);
				} catch (OperationCanceledException) {
					AnsiConsole.MarkupLine("[red]Operation cancelled.[/]");
					break;
				}
			}

			if (cancellationToken.IsCancellationRequested) {
				AnsiConsole.MarkupLine("[red]Blocking UI thread...[/]");
				uiBlocker.WaitOne();
				return;
			}

			if (Client.user != null && Client.client != null && !String.IsNullOrEmpty(message)) {
				Message toSend = new Message(Client.user, message);
				Client.Send(Client.client, toSend.ToJSON());
				Client.sendEvent.WaitOne();
			}
		}

		public static void serverDisconnect() {
			refreshCancellationToken();
			AnsiConsole.Clear();
			AnsiConsole.MarkupLine("[red]Disconnected from server.[/]");
			UI.waitForKey("[grey]Press any key to exit...[/]");
			AnsiConsole.Clear();
		}

		public static User getUser() {
			AnsiConsole.Clear();
			String username = "";

			while (username == "") {
				username = AnsiConsole.Ask<string>("Please enter your username:");
			}
			return new User(username);
		}
		
		public static int getPort() {
			int port;
			while(true) {
				AnsiConsole.Clear();

				AnsiConsole.MarkupLine("[grey bold]Server Connection Settings[/]");
				AnsiConsole.MarkupLine("[red bold]Only use numbers between 0 and 65535[/]");
				String portInput = AnsiConsole.Prompt(new TextPrompt<String>("Please enter a port or q to cancel").DefaultValue("11000"));

				if (portInput == "q") {
					port = -1;
					break;
				}

				bool isNumeric = int.TryParse(portInput, out port);

				if (!isNumeric || (isNumeric && port < 0 || isNumeric && port > 65535)) {
					AnsiConsole.MarkupLine("[red]Invalid port.[/]");
					UI.waitForKey("[grey]Press any key to return to try again...[/]");
					continue;
				} 
				break;
			}
			return port;
		}

		public static IPEndPoint getIpEndpoint() {
			IPAddress ipAddress;
			int port;
			String ip;

			while (true) {
				AnsiConsole.Clear();
				AnsiConsole.MarkupLine("[grey bold]Client Connection Settings[/]");
				AnsiConsole.MarkupLine("[red bold]Only use IPV4 addresses[/]");
				ip = AnsiConsole.Prompt(new TextPrompt<String>("IP Address").DefaultValue("localhost"));

				if (ip == "localhost") {
					IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
					ipAddress = ipHostInfo.AddressList[0];
					ip = $"{ipAddress.ToString()} (localhost)";
					break;
				} 

				if (!IPAddress.TryParse(ip, out ipAddress!)) {
					AnsiConsole.MarkupLine("[red]Invalid IP Address.[/]");
					UI.waitForKey("[grey]Press any key to return to try again...[/]");
					continue;
				}
				break;
			}

			while (true) {
				AnsiConsole.Clear();
				AnsiConsole.MarkupLine("[grey bold]Connection Settings[/]");
				AnsiConsole.MarkupLine($"[grey]IP: {ip}[/]");
				String portString = AnsiConsole.Prompt(new TextPrompt<String>("Port").DefaultValue("11000"));
				bool isNumeric = int.TryParse(portString, out port);
				if (!isNumeric || (isNumeric && port < 0 || isNumeric && port > 65535)) {
					AnsiConsole.MarkupLine("[red]Invalid port.[/]");
					UI.waitForKey("[grey]Press any key to return to try again...[/]");
					continue;
				} 
				break;
			}

			AnsiConsole.Clear();
			AnsiConsole.MarkupLine("[grey bold]Connection Settings[/]");
			AnsiConsole.MarkupLine($"[grey]IP: {ip}[/]");
			AnsiConsole.MarkupLine($"[grey]Port: {port}[/]");
			UI.waitForKey("[grey]Press any key to continue...[/]");
			return new IPEndPoint(ipAddress, port);
		}
	}
}
