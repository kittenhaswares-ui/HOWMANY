using HowMany.Plugin.Models;
using HowMany.Plugin.Services;
using HowMany.Plugin.UI;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace HowMany.Plugin;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/howmany";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IChatGui chatGui;
    private readonly IPluginLog log;
    private readonly PluginConfiguration configuration;
    private readonly WindowSystem windowSystem = new("HOWMANY");
    private readonly TargetTracker targetTracker;
    private readonly CounterWindow counterWindow;
    private readonly SettingsWindow settingsWindow;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IClientState clientState,
        IObjectTable objectTable,
        IFramework framework,
        ITextureProvider textureProvider,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.chatGui = chatGui;
        this.log = log;

        configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        configuration.Initialize(pluginInterface);

        targetTracker = new TargetTracker(clientState, objectTable, framework, configuration);
        counterWindow = new CounterWindow(configuration, targetTracker, textureProvider);
        settingsWindow = new SettingsWindow(configuration, counterWindow, configuration.Save);
        windowSystem.AddWindow(counterWindow);
        windowSystem.AddWindow(settingsWindow);

        commandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open HOWMANY settings. Subcommands: show, hide, lock, unlock, preview, reset, help.",
        });

        pluginInterface.UiBuilder.Draw += Draw;
        pluginInterface.UiBuilder.OpenMainUi += OpenSettings;
        pluginInterface.UiBuilder.OpenConfigUi += OpenSettings;
        targetTracker.RefreshNow();
    }

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= Draw;
        pluginInterface.UiBuilder.OpenMainUi -= OpenSettings;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenSettings;
        commandManager.RemoveHandler(Command);
        targetTracker.Dispose();
        windowSystem.RemoveAllWindows();
    }

    private void Draw() => windowSystem.Draw();

    private void OpenSettings() => settingsWindow.IsOpen = true;

    private void OnCommand(string _, string arguments)
    {
        try
        {
            HandleCommand(arguments.Trim());
        }
        catch (Exception exception)
        {
            log.Error(exception, "HOWMANY command failed.");
            chatGui.PrintError("[HOWMANY] The command failed. See the Dalamud log for details.");
        }
    }

    private void HandleCommand(string arguments)
    {
        switch (arguments.ToLowerInvariant())
        {
            case "":
            case "open":
            case "config":
                settingsWindow.Toggle();
                return;
            case "show":
                configuration.Enabled = true;
                break;
            case "hide":
                configuration.Enabled = false;
                break;
            case "lock":
                configuration.Locked = true;
                break;
            case "unlock":
                configuration.Locked = false;
                break;
            case "preview":
                counterWindow.PreviewEnabled = !counterWindow.PreviewEnabled;
                chatGui.Print($"[HOWMANY] Preview {(counterWindow.PreviewEnabled ? "enabled" : "disabled")} for this session.");
                return;
            case "reset":
                configuration.ResetToDefaults();
                counterWindow.PreviewEnabled = false;
                counterWindow.ResetWindowPosition();
                break;
            case "help":
                PrintHelp();
                return;
            default:
                PrintHelp(true);
                return;
        }

        configuration.Save();
        chatGui.Print($"[HOWMANY] {arguments.ToLowerInvariant()} applied.");
    }

    private void PrintHelp(bool error = false)
    {
        const string text = "Usage: /howmany [show|hide|lock|unlock|preview|reset|help]. /howmany opens settings.";
        if (error) chatGui.PrintError($"[HOWMANY] {text}");
        else chatGui.Print($"[HOWMANY] {text}");
    }
}
