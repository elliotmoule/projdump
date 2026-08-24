using System.Text.Json;

namespace projdump.Shared;

public static class CommandHistoryStore
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetDefaultFilePath()
    {
        string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataDir, "projdump", "command-history.json");
    }

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

    // Appends and rewrites the whole file - no cap, no rotation, grows indefinitely by design.
    public static void Save(SavedCommand command, string? filePath = null)
    {
        filePath ??= GetDefaultFilePath();
        var history = Load(filePath);
        history.Add(command);

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, JsonSerializer.Serialize(history, JsonOptions));
    }
}