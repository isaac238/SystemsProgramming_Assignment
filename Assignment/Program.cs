using Spectre.Console;

namespace SystemsProgramming.Assigment {
	class Program {
		public static void Main(string[] args) {
			String type = "";
			Boolean correctArg = false;
			if (args.Length > 0) {
				if (args[0] == "server" || args[0] == "s") {
					correctArg = true;
					Server.Start();
				}

				if (args[0] == "client" || args[0] == "c") {
					correctArg = true;
					Client.Start();
				}

				if (args[0] == "-v" || args[0] == "--version") {
					correctArg = true;
					AnsiConsole.MarkupLine("[green]Version 1.0.0[/]");
					return;
				}

				if (!correctArg) {
					AnsiConsole.MarkupLine("[red bold]Invalid argument use[/]:\n server (s) >> Start a Server\n client (c) >> Start a Client\n --version (-v) >> Check the current version");
					return;
				}
			}

			while (true) {
				AnsiConsole.Clear();
				AnsiConsole.Write(
					new FigletText("Chat App").Color(Color.Red)
				);

				if (String.IsNullOrEmpty(type)) {
					type = AnsiConsole.Prompt(
							new SelectionPrompt<string>()
							.Title("Select an option:")
							.AddChoices(new [] {"Server", "Client", "Exit"})
							);
				}
				
				if (type == "Server") {
					type = "";
					Server.Start();
				}

				if (type == "Client") {
					type = "";
					Client.Start();
				}
				
				if (type == "Exit") {
					type = "";
					AnsiConsole.Clear();
					break;
				}
			}
			AnsiConsole.MarkupLine("[green]Application closed.[/]");

		}
	}
}
