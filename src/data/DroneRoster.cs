namespace BuzzBets.Data;

using Models;

internal static class DroneRoster
{
	public static List<Drone> LoadFromJson(string path)
	{
		string json = File.ReadAllText(path);
		JsonDocument doc = JsonDocument.Parse(json);

		List<Drone> drones = [];
		drones.AddRange(
			doc.RootElement.GetProperty("drones")
				.EnumerateArray()
				.Select(element => new Drone(
					name: element.GetProperty("name").GetString() ?? string.Empty,
					speed: element.GetProperty("speed").GetInt32(),
					acceleration: element.GetProperty("acceleration").GetInt32(),
					reliability: element.GetProperty("reliability").GetInt32(),
					agility: element.GetProperty("agility").GetInt32(),
					archetype: element.GetProperty("archetype").GetString() ?? string.Empty
				))
		);

		return drones;
	}
}
