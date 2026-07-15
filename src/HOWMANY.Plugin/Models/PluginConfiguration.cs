using Dalamud.Configuration;
using Dalamud.Plugin;

namespace HowMany.Plugin.Models;

public sealed class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
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

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface value) => pluginInterface = value;

    public void Save() => pluginInterface?.SavePluginConfig(this);

    public void ResetToDefaults()
    {
        Version = 1;
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
    }
}
