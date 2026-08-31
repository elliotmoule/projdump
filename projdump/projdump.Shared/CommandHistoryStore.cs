using System.Text.Json;

namespace projdump.Shared;

public static class CommandHistoryStore
{
	static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	/// <summary>
	/// Resolves the default history file location under the current user's %APPDATA%.
	/// </summary>
	/// <returns>The full path to command-history.json.</returns>
	public static string GetDefaultFilePath()
	{
		string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(appDataDir, "projdump", "command-history.json");
	}

	/// <summary>
	/// Reads the stored command history, most recently used first.
	/// </summary>
	/// <param name="filePath">History file to read, or null for the default location.</param>
	/// <returns>The stored commands, or an empty list when the file is missing or unreadable.</returns>
	// Never throws on a missing or corrupt file - callers treat "no history" and
	// "unreadable history" the same way, since either just means starting fresh.
	public static List<SavedCommand> Load(string? filePath = null)
	{
		filePath ??= GetDefaultFilePath();
		if (!File.Exists(filePath))
			return [];

		try
		{
			string json = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<List<SavedCommand>>(json, JsonOptions) ?? [];
		}
		catch (JsonException)
		{
			return [];
		}
	}

	/// <summary>
	/// Records a command as the most recently used, promoting it to the top of the history.
	/// </summary>
	/// <param name="command">The command that was just run.</param>
	/// <param name="filePath">History file to write, or null for the default location.</param>
	// Any existing entry with identical options is dropped first, so re-running a command
	// moves it rather than duplicating it. No cap, no rotation - grows indefinitely by design.
	public static void RecordUse(SavedCommand command, string? filePath = null)
	{
		filePath ??= GetDefaultFilePath();

		var history = Load(filePath);
		history.RemoveAll(existing => existing.HasSameOptions(command));
		history.Insert(0, command);

		string? dir = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		File.WriteAllText(filePath, JsonSerializer.Serialize(history, JsonOptions));
	}

	/// <summary>
	/// Finds a stored command matching the given options exactly.
	/// </summary>
	/// <param name="command">The command to look for.</param>
	/// <param name="filePath">History file to read, or null for the default location.</param>
	/// <returns>The matching stored command, or null when it isn't in history.</returns>
	public static SavedCommand? FindMatch(SavedCommand command, string? filePath = null) =>
		Load(filePath).FirstOrDefault(existing => existing.HasSameOptions(command));
}