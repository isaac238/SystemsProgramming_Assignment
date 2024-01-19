using System.Text.Json;
using Spectre.Console;
using System.Text.Json.Serialization;

namespace SystemsProgramming.Assigment {
	[Serializable]
	class Message {
		public string content;
		public User sender;
		public DateTime timestamp;

		public Message(User sender, string content) {
			this.sender = sender;
			this.content = content;
			this.timestamp = DateTime.Now;
		}

		[JsonConstructor]
		public Message() { }

		public string ToJSON() {
			JsonSerializerOptions options = new JsonSerializerOptions {
				WriteIndented = true,
				IncludeFields = true,
			};

			String json = JsonSerializer.Serialize<Message>(this, options);
			json += "<|TYPE|>MESSAGE";
			return json;
		}

		public Panel ToPanel() {
			return new Panel(
				new Columns(
					new Markup(this.content).LeftJustified(),
					new Markup(this.timestamp.ToString("dd/MM/yy HH:mm")).RightJustified()
				)
			).Header(this.sender.username)
			.Border(BoxBorder.Rounded)
			.Expand();
		}
	}
}
