using System.Net.Sockets;
using System.Globalization;
using System.Text;

namespace SystemsProgramming.Assigment {
	interface CommandImplementation {
		public Message Execute(Socket handler);
	}

	class UsersCommandImplementation : CommandImplementation {
		public Message Execute(Socket handler) {
			List<User> users = Server.users.ToList();
			Message response = new Message(
				new User("Server - [bold]Users List[/]"),
				$"There are [bold underline]{users.Count} users[/] connected to the server.\n\n{String.Join("\n", users.Select(user => $"- [bold]{user.username}[/]"))}"
			);

			return response;
		}
	}

	class HelpCommandImplementation : CommandImplementation {
		public Message Execute(Socket handler) {
			StringBuilder builder = new StringBuilder();
			builder.Append("[bold underline]Available commands:[/]\n");

			TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;
			Server.commands.ForEach(command => {
				builder.Append($"\n[bold]{textInfo.ToTitleCase(command.name)}[/]\n");
				builder.Append($"[grey]Aliases:[/] [red]{String.Join(", ", command.aliases)}[/]\n");
				builder.Append($"[grey]Description:[/] [red]{command.description}[/]\n");
			});

			return new Message(new User("Server - [bold]Help Menu[/]"), builder.ToString());
		}
	}

	class ExitCommandImplementation : CommandImplementation {
		public Message Execute(Socket handler) {
			Boolean success = Server.removalQueue.Add(handler);

			if (success) Console.WriteLine("Added handler to removal list.");
			if (!success) Console.WriteLine("Failed to add handler to remove list.");

			return new Message(new User("Server - [bold]Exit Command[/]"), "Disconnecting from server.");
		}
	}
}
