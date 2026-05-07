using System.IO;
using System.Text.Json;

namespace Moloko.Services;

public sealed class DatabaseSettings
{
    public bool UsePostgreSql { get; set; } = true;
    public string ConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=moloko;Username=postgres;Password=12345678";

    public static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "database.json");

    public static DatabaseSettings LoadOrCreate()
    {
        if (!File.Exists(SettingsPath))
        {
            var settings = new DatabaseSettings();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            return settings;
        }

        var content = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<DatabaseSettings>(content) ?? new DatabaseSettings();
    }
}
