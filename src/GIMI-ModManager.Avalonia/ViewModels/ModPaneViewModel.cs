using System.Collections.ObjectModel;
using global::Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Entities;
using GIMI_ModManager.Core.Entities.Mods.Contract;
using GIMI_ModManager.Core.Helpers;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

/// <summary>
/// The right-hand detail pane for a single selected mod: cover image, editable metadata
/// (name/author/url/description), and the 3Dmigoto key-swap editor.
/// </summary>
public partial class ModPaneViewModel : ViewModelBase
{
    private readonly IFilePickerService _filePicker;
    private readonly INotificationService _notifications;
    private readonly ILogger _logger;

    private ISkinMod? _mod;
    private ModSettings? _settings;

    public event Action? MetadataSaved;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _modName = string.Empty;
    [ObservableProperty] private string? _coverImagePath;
    [ObservableProperty] private string? _customName;
    [ObservableProperty] private string? _author;
    [ObservableProperty] private string? _modUrl;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private bool _hasKeySwaps;

    private string? _modActiveVar;
    private bool _suppressModApply;

    // One three-position switch for the whole mod (applies to all its switchable skin-cycle keys).
    [ObservableProperty] private KeySwapState _modKeySwapState;
    [ObservableProperty] private bool _hasSwitchableKeySwaps;
    [ObservableProperty] private bool _modCanForeground;

    public bool IsModUnrestricted
    {
        get => ModKeySwapState == KeySwapState.Unrestricted;
        set { if (value && !_suppressModApply) _ = ApplyModStateAsync(KeySwapState.Unrestricted); }
    }

    public bool IsModForegroundOnly
    {
        get => ModKeySwapState == KeySwapState.ForegroundOnly;
        set { if (value && !_suppressModApply) _ = ApplyModStateAsync(KeySwapState.ForegroundOnly); }
    }

    public bool IsModDisabled
    {
        get => ModKeySwapState == KeySwapState.Disabled;
        set { if (value && !_suppressModApply) _ = ApplyModStateAsync(KeySwapState.Disabled); }
    }

    partial void OnModKeySwapStateChanged(KeySwapState value)
    {
        OnPropertyChanged(nameof(IsModUnrestricted));
        OnPropertyChanged(nameof(IsModForegroundOnly));
        OnPropertyChanged(nameof(IsModDisabled));
    }

    public ObservableCollection<KeySwapItem> KeySwaps { get; } = new();

    public ModPaneViewModel(IFilePickerService filePicker, INotificationService notifications, ILogger logger)
    {
        _filePicker = filePicker;
        _notifications = notifications;
        _logger = logger.ForContext<ModPaneViewModel>();
    }

    public void Clear()
    {
        IsVisible = false;
        _mod = null;
        _settings = null;
        KeySwaps.Clear();
        HasKeySwaps = false;
    }

    public async Task LoadAsync(ISkinMod mod)
    {
        _mod = mod;
        ModName = mod.GetDisplayName();

        try
        {
            _settings = await mod.Settings.ReadSettingsAsync();
            CustomName = _settings.CustomName;
            Author = _settings.Author;
            ModUrl = _settings.ModUrl?.ToString();
            Description = _settings.Description;
            CoverImagePath = _settings.ImagePath?.LocalPath;
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed to read settings for {Mod}", mod.Name);
        }

        KeySwaps.Clear();
        try
        {
            if (mod.KeySwaps is not null)
            {
                // The mod's on-screen-character variable ($object_detected/$active) — gates "仅前台".
                _modActiveVar = await mod.KeySwaps.DetectActiveVariableAsync();
                var all = await mod.KeySwaps.ReadAllKeySwapConfigurations();
                foreach (var (iniFile, sections) in all)
                    foreach (var section in sections)
                        KeySwaps.Add(new KeySwapItem(iniFile, section, _modActiveVar));
            }
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed to read key swaps for {Mod}", mod.Name);
        }

        HasKeySwaps = KeySwaps.Count > 0;
        RecomputeModKeySwapState();
        IsVisible = true;
    }

    private void RecomputeModKeySwapState()
    {
        var switchable = KeySwaps.Where(k => k.IsSwitchable).ToList();
        HasSwitchableKeySwaps = switchable.Count > 0;
        ModCanForeground = !string.IsNullOrEmpty(_modActiveVar);

        // If every switchable key already agrees, show that; otherwise default to 无限制 (picking one unifies them).
        var common = switchable.Count > 0 && switchable.All(k => k.State == switchable[0].State)
            ? switchable[0].State
            : KeySwapState.Unrestricted;

        _suppressModApply = true;
        ModKeySwapState = common;
        _suppressModApply = false;
    }

    /// <summary>
    /// Apply one three-position state (无限制 / 仅前台 / 禁用) to ALL of the mod's switchable skin-cycle keys
    /// at once. Author/menu conditions are left alone. Each touched .ini is backed up as <c>*.bak-keyswap</c>.
    /// </summary>
    private async Task ApplyModStateAsync(KeySwapState state)
    {
        if (_mod?.KeySwaps is null)
            return;

        var byFile = new Dictionary<string, List<KeySwapSection>>();
        foreach (var item in KeySwaps.Where(k => k.IsSwitchable))
        {
            item.State = state;
            if (!byFile.TryGetValue(item.IniFile, out var list))
            {
                list = new List<KeySwapSection>();
                byFile[item.IniFile] = list;
            }

            list.Add(item.ToSection());
        }

        _suppressModApply = true;
        ModKeySwapState = state;
        _suppressModApply = false;

        if (byFile.Count == 0)
            return;

        try
        {
            foreach (var iniFile in byFile.Keys)
            {
                var path = Path.Combine(_mod.FullPath, iniFile);
                var backup = path + ".bak-keyswap";
                if (File.Exists(path) && !File.Exists(backup))
                    File.Copy(path, backup);
            }

            await _mod.KeySwaps.SaveAllKeySwapConfigurations(byFile);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to apply key swap state for {Mod}", _mod.Name);
            _notifications.ShowError("保存失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task SaveMetadata()
    {
        if (_mod is null || _settings is null)
            return;

        try
        {
            var updated = _settings.DeepCopyWithProperties(
                customName: NewValue<string?>.Set(Empty(CustomName)),
                author: NewValue<string?>.Set(Empty(Author)),
                modUrl: NewValue<Uri?>.Set(TryUri(ModUrl)),
                description: NewValue<string?>.Set(Empty(Description)));

            await _mod.Settings.SaveSettingsAsync(updated);
            _settings = updated;
            ModName = _mod.GetDisplayName();
            _notifications.ShowSuccess("已保存", "模组信息已更新。");
            MetadataSaved?.Invoke();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save mod metadata for {Mod}", _mod.Name);
            _notifications.ShowError("保存失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task SetCoverImage()
    {
        if (_mod is null || _settings is null)
            return;

        var filters = new[]
        {
            new FilePickerFileType("图片") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.gif" } }
        };
        var path = await _filePicker.PickFileAsync("选择封面图片", filters);
        if (path is null)
            return;

        try
        {
            var updated = _settings.DeepCopyWithProperties(imagePath: NewValue<Uri?>.Set(new Uri(path)));
            await _mod.Settings.SaveSettingsAsync(updated);
            _settings = await _mod.Settings.ReadSettingsAsync();
            CoverImagePath = _settings.ImagePath?.LocalPath;
            _notifications.ShowSuccess("已更新封面", "模组封面已设置。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to set cover image for {Mod}", _mod.Name);
            _notifications.ShowError("设置封面失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task SaveKeySwaps()
    {
        if (_mod?.KeySwaps is null || KeySwaps.Count == 0)
            return;

        try
        {
            var byFile = KeySwaps
                .GroupBy(k => k.IniFile)
                .ToDictionary(g => g.Key, g => g.Select(k => k.ToSection()).ToList());

            await _mod.KeySwaps.SaveAllKeySwapConfigurations(byFile);
            _notifications.ShowSuccess("已保存", "按键切换配置已写入 .ini。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save key swaps for {Mod}", _mod.Name);
            _notifications.ShowError("保存失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task CopyPath()
    {
        if (_mod is not null)
        {
            await ClipboardHelper.SetTextAsync(_mod.FullPath);
            _notifications.ShowInfo("已复制", "模组路径已复制到剪贴板。");
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (_mod is not null)
            PlatformService.OpenInFileManager(_mod.FullPath);
    }

    [RelayCommand]
    private void OpenUrl()
    {
        if (!string.IsNullOrWhiteSpace(ModUrl))
            PlatformService.OpenUrl(ModUrl);
    }

    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Uri? TryUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ? uri : null;
    }
}
