namespace GIMI_ModManager.Core.Entities.Mods.FileModels;

public class IniKeySwapSection
{
    public Dictionary<string, string> IniKeyValues { get; } = new();

    public List<string> ForwardKeys { get; set; } = [];
    public List<string> BackwardKeys { get; set; } = [];

    public const string KeySwapIniSection = "KeySwap";

    public const string CommandListSection = "KeyCommndList";
    public string SectionKey { get; set; } = KeySwapIniSection;

    public const string ForwardIniKey = "key";


    public const string BackwardIniKey = "back";


    public const string TypeIniKey = "type";

    public string? Type
    {
        get => IniKeyValues.GetValueOrDefault(TypeIniKey);
        set => IniKeyValues[TypeIniKey] = value ?? string.Empty;
    }

    public const string ConditionIniKey = "condition";

    /// <summary>
    /// The 3dmigoto <c>condition</c> gate on this key. Controls whether the key fires at all:
    /// absent = fires globally (all mods on this key); a character-active var (e.g.
    /// <c>$object_detected</c>) = only when that character is on screen; <c>0</c> = never (disabled).
    /// </summary>
    public string? Condition
    {
        get => IniKeyValues.GetValueOrDefault(ConditionIniKey);
        set
        {
            if (value is null)
                IniKeyValues.Remove(ConditionIniKey);
            else
                IniKeyValues[ConditionIniKey] = value;
        }
    }

    public const string SwapVarIniKey = "$swapvar";
    public string[]? SwapVar { get; set; }

    public bool AnyValues()
    {
        return ForwardKeys.Count > 0 || BackwardKeys.Count > 0;
    }
}