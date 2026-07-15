using System.Numerics;
using HowMany.Plugin.Models;
using HowMany.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace HowMany.Plugin.UI;

internal sealed class SettingsWindow : Window
{
    private readonly PluginConfiguration configuration;
    private readonly CounterWindow counterWindow;
    private readonly TargetTracker tracker;
    private readonly Action save;

    public SettingsWindow(
        PluginConfiguration configuration,
        CounterWindow counterWindow,
        TargetTracker tracker,
        Action save)
        : base("HOWMANY settings###HOWMANYSettings")
    {
        this.configuration = configuration;
        this.counterWindow = counterWindow;
        this.tracker = tracker;
        this.save = save;

        Size = new Vector2(515, 650);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(410, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        ImGui.TextColored(new Vector4(0.46f, 0.82f, 1f, 1f), "HOWMANY");
        ImGui.TextWrapped("Shows how many visible opponents are focusing or actively pressuring you, with one official job icon per opponent.");
        ImGui.Spacing();

        DrawCheckbox("Enable overlay", configuration.Enabled, value => configuration.Enabled = value);
        DrawCheckbox("Lock overlay", configuration.Locked, value => configuration.Locked = value);
        if (configuration.Locked)
        {
            DrawCheckbox("Click-through while locked", configuration.ClickThroughWhenLocked, value => configuration.ClickThroughWhenLocked = value);
        }

        DrawCheckbox("Show background", configuration.ShowBackground, value => configuration.ShowBackground = value);
        DrawCheckbox("Show enemy job icons", configuration.ShowJobIcons, value => configuration.ShowJobIcons = value);
        DrawCheckbox("Threat colors (0 gray, 2 amber, 3+ red)", configuration.UseThreatColors, value => configuration.UseThreatColors = value);
        DrawCheckbox("Include Wolves' Den Pier", configuration.IncludeWolvesDen, value => configuration.IncludeWolvesDen = value);
        var preview = counterWindow.PreviewEnabled;
        if (ImGui.Checkbox("Preview outside PvP (this session only)", ref preview))
        {
            counterWindow.PreviewEnabled = preview;
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Crystalline Conflict detection");
        DrawSlider(
            "Recent pressure duration",
            configuration.PressureWindowSeconds,
            0.5f,
            8f,
            value => configuration.PressureWindowSeconds = value,
            "%.1f s");
        ImGui.TextWrapped("CC does not always expose an opponent's current hard target. HOWMANY therefore keeps an opponent visible briefly after a harmful attempt hits, misses, is blocked, resisted, or absorbed by Guard. Heals and buffs do not count.");

        ImGui.Separator();
        ImGui.TextUnformatted("Appearance");
        DrawSlider("Number size", configuration.NumberScale, 1f, 5f, value => configuration.NumberScale = value, "%.1fx");
        DrawSlider("Job icon size", configuration.IconSize, 18f, 72f, value => configuration.IconSize = value, "%.0f px");
        DrawSlider("Icon spacing", configuration.IconSpacing, 0f, 20f, value => configuration.IconSpacing = value, "%.0f px");
        DrawIntegerSlider("Icons per row", configuration.IconsPerRow, 1, 16, value => configuration.IconsPerRow = value);
        if (configuration.ShowBackground)
        {
            DrawSlider("Background opacity", configuration.BackgroundOpacity, 0.1f, 1f, value => configuration.BackgroundOpacity = value, "%.0f%%", 100f);
        }

        ImGui.Spacing();
        if (ImGui.Button("Reset position")) counterWindow.ResetWindowPosition();
        ImGui.SameLine();
        if (ImGui.Button("Reset all settings"))
        {
            configuration.ResetToDefaults();
            counterWindow.PreviewEnabled = false;
            counterWindow.ResetWindowPosition();
            save();
        }

        ImGui.Separator();
        ImGui.TextDisabled("Detection notes");
        ImGui.TextWrapped("The display combines visible hostile hard/cast targets with recent harmful pressure. AoE hits can briefly count an opponent even when their hard target is someone else. Enemies outside client range are not detectable.");
        ImGui.TextWrapped("No character name, account identifier, or combat history is saved or sent. HOWMANY makes no network requests.");

        if (ImGui.CollapsingHeader("Live diagnostics"))
        {
            var value = tracker.Diagnostics;
            ImGui.TextUnformatted(value.IsActive ? "PvP tracker: active" : "PvP tracker: inactive");
            ImGui.TextUnformatted($"Territory: {value.TerritoryId}");
            ImGui.TextUnformatted($"Visible players: {value.VisiblePlayers}");
            ImGui.TextUnformatted($"Hard targets / casts: {value.NativeHardTargetMatches} / {value.CastTargetMatches}");
            ImGui.TextUnformatted($"Recent pressure / shown: {value.RecentPressurePlayers} / {value.DisplayedPlayers}");
            ImGui.TextUnformatted($"Pressure capture: {(value.PressureCaptureAvailable ? "ready" : "unavailable")}");
            ImGui.TextDisabled("No names or identifiers are included. The same line is available with /howmany debug.");
        }
    }

    private void DrawCheckbox(string label, bool current, Action<bool> assign)
    {
        var value = current;
        if (!ImGui.Checkbox(label, ref value)) return;
        assign(value);
        save();
    }

    private void DrawSlider(
        string label,
        float current,
        float minimum,
        float maximum,
        Action<float> assign,
        string format,
        float displayMultiplier = 1f)
    {
        var value = current;
        var displayed = value * displayMultiplier;
        if (displayMultiplier == 1f)
        {
            if (!ImGui.SliderFloat(label, ref value, minimum, maximum, format)) return;
        }
        else
        {
            if (!ImGui.SliderFloat(label, ref displayed, minimum * displayMultiplier, maximum * displayMultiplier, format)) return;
            value = displayed / displayMultiplier;
        }

        assign(value);
        save();
    }

    private void DrawIntegerSlider(string label, int current, int minimum, int maximum, Action<int> assign)
    {
        var value = current;
        if (!ImGui.SliderInt(label, ref value, minimum, maximum)) return;
        assign(value);
        save();
    }
}
