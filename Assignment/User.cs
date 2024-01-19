using System.Text.Json.Serialization;
using System.Text.Json;

namespace SystemsProgramming.Assigment {
	[Serializable]
	class User {
		public string username;

		public User(string username) {
			this.username = username;
		}

		[JsonConstructor]
		public User() { }

		public string ToJSON() {
			JsonSerializerOptions options = new JsonSerializerOptions {
				WriteIndented = true,
				IncludeFields = true,
			};

			String json = JsonSerializer.Serialize<User>(this, options);
			json += "<|TYPE|>USER";
			return json;
		}
	}
}
