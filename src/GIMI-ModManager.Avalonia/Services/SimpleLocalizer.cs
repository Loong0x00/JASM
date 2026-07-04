using System.Diagnostics.CodeAnalysis;
using GIMI_ModManager.Core.Contracts.Services;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Minimal <see cref="ILanguageLocalizer"/> used by the Core GameService to pick which
/// Languages/&lt;code&gt;/ asset overrides to load. This fork bundles Chinese (zh-cn) name data,
/// so we default to that. UI strings themselves are provided directly in the AXAML views.
/// </summary>
public class SimpleLocalizer : ILanguageLocalizer
{
    public event EventHandler? LanguageChanged;

    public SimpleLocalizer(string languageCode = "zh-cn")
    {
        CurrentLanguage = new Language(languageCode);
        FallbackLanguage = new Language("en");
        AvailableLanguages = new List<ILanguage> { new Language("zh-cn"), new Language("en") };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public ILanguage CurrentLanguage { get; private set; }
    public ILanguage FallbackLanguage { get; }
    public IReadOnlyList<ILanguage> AvailableLanguages { get; }

    public Task SetLanguageAsync(ILanguage language)
    {
        CurrentLanguage = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SetLanguageAsync(string languageCode) => SetLanguageAsync(new Language(languageCode));

    public string GetLocalizedString(string uid) => uid;

    [return: NotNullIfNotNull(nameof(defaultValue))]
    public string? GetLocalizedStringOrDefault(string uid, string? defaultValue = null,
        bool? useUidAsDefaultValue = null)
    {
        if (defaultValue is not null)
            return defaultValue;

        return useUidAsDefaultValue is false ? null : uid;
    }
}
