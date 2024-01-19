using System.Net.Sockets;
using Spectre.Console;

namespace SystemsProgramming.Assigment {
	class Command {
		private string _name;
		public string name { get => _name; }
		private Boolean useCaching = false;
		private Message? cachedMessage = null;

		private HashSet<String> _aliases = new HashSet<String>();
		public HashSet<String> aliases { get => _aliases;}
		private string _description;
		public string description { get => _description; }
		CommandImplementation implementation;

		public Command(string name, string description, CommandImplementation implementation, Boolean useCaching = false) {
			this._name = name;
			this._description = description;
			this.implementation = implementation;
			this.useCaching = useCaching;
		}

		public Command(string name, string description, CommandImplementation implementation, HashSet<String> aliases, Boolean useCaching = false) {
			this._name = name;
			this._description = description;
			this.implementation = implementation;
			foreach (String alias in aliases) {
				this._aliases.Add(alias);
			}
			this.useCaching = useCaching;
		}

		public bool AddAlias(string alias) {
			if (Server.commands.Any(command => command.aliases.Contains(alias))) {
				return false;
			}
			return _aliases.Add(alias);
		}

		public Panel ToPanel() {
			return new Panel(
				new Columns(
					new Markup($"[grey]Aliases:[/] [red]{String.Join(", ", this._aliases)}[/]"),
					new Markup($"[grey]Description:[/] [red]{this._description}[/]")
				)
			).Header(this._name).Border(BoxBorder.Rounded).Expand();
		}

		public Message Execute(Socket client) {
			if (this.cachedMessage == null || !this.useCaching) {
				this.cachedMessage = implementation.Execute(client);
			}
			return this.cachedMessage;
		}
	}
}
