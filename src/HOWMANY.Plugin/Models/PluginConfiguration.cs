using Dalamud.Configuration;
using Dalamud.Plugin;

namespace HowMany.Plugin.Models;

public sealed class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 2;
    public bool Enabled { get; set; } = true;
    public bool Locked { get; set; }
    public bool ClickThroughWhenLocked { get; set; } = true;
    public bool ShowBackground { get; set; } = true;
    public bool ShowJobIcons { get; set; } = true;
    public bool UseThreatColors { get; set; } = true;
    public bool IncludeWolvesDen { get; set; }
    public float NumberScale { get; set; } = 2.6f;
    public float IconSize { get; set; } = 38f;
    public float IconSpacing { get; set; } = 4f;
    public float BackgroundOpacity { get; set; } = 0.62f;
    public int IconsPerRow { get; set; } = 5;
    public float PressureWindowSeconds { get; set; } = 3f;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface value)
    {
        pluginInterface = value;
        if (Version >= 2) return;
        Version = 2;
        PressureWindowSeconds = 3f;
        Save();
    }

    public void Save() => pluginInterface?.SavePluginConfig(this);

    public void ResetToDefaults()
    {
        Version = 2;
        Enabled = true;
        Locked = false;
        ClickThroughWhenLocked = true;
        ShowBackground = true;
        ShowJobIcons = true;
        UseThreatColors = true;
        IncludeWolvesDen = false;
        NumberScale = 2.6f;
        IconSize = 38f;
        IconSpacing = 4f;
        BackgroundOpacity = 0.62f;
        IconsPerRow = 5;
        PressureWindowSeconds = 3f;
    }
}
