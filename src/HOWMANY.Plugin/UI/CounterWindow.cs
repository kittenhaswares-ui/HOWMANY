using System.Numerics;
using HowMany.Core;
using HowMany.Plugin.Models;
using HowMany.Plugin.Services;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace HowMany.Plugin.UI;

internal sealed class CounterWindow : Window
{
    private const ImGuiWindowFlags BaseFlags =
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse |
        ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoFocusOnAppearing |
        ImGuiWindowFlags.NoNav |
        ImGuiWindowFlags.NoDocking;

    private static readonly TargetingOpponent[] PreviewTargeters =
    [
        new(1, 32), // DRK
        new(2, 38), // DNC
        new(3, 40), // SGE
    ];

    private readonly PluginConfiguration configuration;
    private readonly TargetTracker tracker;
    private readonly ITextureProvider textureProvider;
    private bool resetPosition;

    public bool PreviewEnabled { get; set; }

    public CounterWindow(
        PluginConfiguration configuration,
        TargetTracker tracker,
        ITextureProvider textureProvider)
        : base("HOWMANY###HOWMANYOverlay")
    {
        this.configuration = configuration;
        this.tracker = tracker;
        this.textureProvider = textureProvider;

        IsOpen = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        Flags = BaseFlags;
        Position = new Vector2(360, 240);
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public override bool DrawConditions() =>
        configuration.Enabled && (tracker.IsActive || PreviewEnabled);

    public override void PreDraw()
    {
        var flags = BaseFlags;
        if (configuration.Locked) flags |= ImGuiWindowFlags.NoMove;
        if (configuration.Locked && configuration.ClickThroughWhenLocked) flags |= ImGuiWindowFlags.NoInputs;
        if (!configuration.ShowBackground) flags |= ImGuiWindowFlags.NoBackground;
        Flags = flags;
        BgAlpha = configuration.ShowBackground ? configuration.BackgroundOpacity : 0f;

        if (!resetPosition) return;
        ImGui.SetNextWindowPos(new Vector2(360, 240), ImGuiCond.Always);
        resetPosition = false;
    }

    public override void Draw()
    {
        var targeters = tracker.IsActive ? tracker.Targeters : PreviewTargeters;
        var count = targeters.Count;
        var countColor = CountColor(count, configuration.UseThreatColors);

        ImGui.SetWindowFontScale(configuration.NumberScale);
        ImGui.TextColored(countColor, count.ToString());
        ImGui.SetWindowFontScale(1f);

        if (!configuration.ShowJobIcons || targeters.Count == 0) return;

        ImGui.SameLine(0, 10f * ImGuiHelpers.GlobalScale);
        ImGui.BeginGroup();
        var iconSize = new Vector2(configuration.IconSize * ImGuiHelpers.GlobalScale);
        var iconsPerRow = Math.Clamp(configuration.IconsPerRow, 1, 16);
        for (var index = 0; index < targeters.Count; index++)
        {
            if (index > 0 && index % iconsPerRow != 0)
            {
                ImGui.SameLine(0, configuration.IconSpacing * ImGuiHelpers.GlobalScale);
            }

            DrawJobIcon(targeters[index].JobId, iconSize);
        }

        ImGui.EndGroup();
    }

    public void ResetWindowPosition() => resetPosition = true;

    private void DrawJobIcon(uint jobId, Vector2 size)
    {
        if (jobId == 0)
        {
            ImGui.Dummy(size);
            return;
        }

        // 62000 + ClassJob.RowId is the transparent official job glyph family.
        // 62100 is the heavier framed/tile variant used by some Dalamud samples.
        var lookup = new GameIconLookup(62000u + jobId);
        if (!textureProvider.TryGetFromGameIcon(lookup, out var shared) ||
            !shared.TryGetWrap(out var wrap, out _))
        {
            ImGui.Dummy(size);
            return;
        }

        ImGui.Image(wrap.Handle, size);
    }

    private static Vector4 CountColor(int count, bool useThreatColors)
    {
        if (!useThreatColors) return Vector4.One;
        return count switch
        {
            0 => new Vector4(0.62f, 0.66f, 0.72f, 1f),
            1 => new Vector4(0.92f, 0.96f, 1f, 1f),
            2 => new Vector4(1f, 0.72f, 0.18f, 1f),
            _ => new Vector4(1f, 0.24f, 0.22f, 1f),
        };
    }
}
