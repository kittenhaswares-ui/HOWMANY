using HowMany.Core;
using HowMany.Plugin.Models;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace HowMany.Plugin.Services;

internal sealed class TargetTracker : IDisposable
{
    private const long UpdateIntervalMilliseconds = 100;

    private readonly IClientState clientState;
    private readonly IObjectTable objectTable;
    private readonly IFramework framework;
    private readonly PluginConfiguration configuration;
    private TargetingOpponent[] targeters = [];
    private long nextUpdateAt;
    private int active;
    private bool started;
    private bool disposed;

    public TargetTracker(
        IClientState clientState,
        IObjectTable objectTable,
        IFramework framework,
        PluginConfiguration configuration)
    {
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.framework = framework;
        this.configuration = configuration;
    }

    public bool IsActive => Volatile.Read(ref active) == 1;

    public IReadOnlyList<TargetingOpponent> Targeters => Volatile.Read(ref targeters);

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) return;
        started = true;
        framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (started) framework.Update -= OnFrameworkUpdate;
        Publish(false, []);
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        if (disposed || Environment.TickCount64 < nextUpdateAt) return;
        nextUpdateAt = Environment.TickCount64 + UpdateIntervalMilliseconds;
        UpdateSnapshot();
    }

    private void UpdateSnapshot()
    {
        var inSupportedPvP = clientState.IsPvPExcludingDen ||
                             (configuration.IncludeWolvesDen && clientState.IsPvP);
        if (!inSupportedPvP)
        {
            Publish(false, []);
            return;
        }

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer is null)
        {
            Publish(false, []);
            return;
        }

        var observations = new List<PlayerObservation>();
        foreach (var battleCharacter in objectTable.PlayerObjects)
        {
            if (battleCharacter is not IPlayerCharacter player) continue;
            observations.Add(new PlayerObservation(
                player.GameObjectId,
                player.TargetObjectId,
                player.ClassJob.IsValid ? player.ClassJob.RowId : 0,
                (player.StatusFlags & StatusFlags.Hostile) != 0,
                player.IsDead));
        }

        var snapshot = TargetingSnapshot.Build(localPlayer.GameObjectId, observations);
        Publish(true, snapshot.Opponents.ToArray());
    }

    private void Publish(bool isActive, TargetingOpponent[] value)
    {
        Interlocked.Exchange(ref targeters, value);
        Volatile.Write(ref active, isActive ? 1 : 0);
    }
}
