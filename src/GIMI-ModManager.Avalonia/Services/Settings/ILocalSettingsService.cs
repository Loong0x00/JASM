namespace GIMI_ModManager.Avalonia.Services.Settings;

public interface ILocalSettingsService
{
    /// <summary>Full path to the game-scoped settings file (JASM/ApplicationData_&lt;Game&gt;/LocalSettings.json).</summary>
    string GameScopedSettingsLocation { get; }

    /// <summary>The game-scoped application-data folder (e.g. JASM/ApplicationData_Genshin).</summary>
    string ApplicationDataFolder { get; }

    void SetApplicationDataFolderName(string folderName);

    Task<T?> ReadSettingAsync<T>(string key, SettingScope settingScope = SettingScope.Game);

    Task<T> ReadOrCreateSettingAsync<T>(string key, SettingScope settingScope = SettingScope.Game)
        where T : new();

    Task SaveSettingAsync<T>(string key, T value, SettingScope settingScope = SettingScope.Game)
        where T : notnull;

    T? ReadSetting<T>(string key, SettingScope settingScope = SettingScope.Game);
}
