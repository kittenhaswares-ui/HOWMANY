using HowMany.Core;
using HowMany.Plugin.Models;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HowMany.Plugin.Services;

internal sealed class TargetTracker : IDisposable
{
    private const long UpdateIntervalMilliseconds = 100;

    private readonly IClientState clientState;
    private readonly IObjectTable objectTable;
    private readonly IFramework framework;
    private readonly PluginConfiguration configuration;
    private readonly IPluginLog log;
    private readonly ActionPressureCapture? pressureCapture;
    private readonly Dictionary<ulong, long> recentPressure = [];
    private TargetingOpponent[] targeters = [];
    private TargetTrackerDiagnostics diagnostics = TargetTrackerDiagnostics.Inactive(0, false);
    private long nextUpdateAt;
    private int active;
    private bool started;
    private bool disposed;

    public TargetTracker(
        IClientState clientState,
        IObjectTable objectTable,
        IFramework framework,
        IGameInteropProvider interop,
        IPluginLog log,
        PluginConfiguration configuration)
    {
        this.clientState = clientState;
        this.objectTable = objectTable;
        this.framework = framework;
        this.log = log;
        this.configuration = configuration;

        try
        {
            pressureCapture = new ActionPressureCapture(interop, log);
        }
        catch (Exception exception)
        {
            log.Error(exception, "HOWMANY could not initialize recent-pressure capture. Hard-target detection remains available.");
        }
    }

    public bool IsActive => Volatile.Read(ref active) == 1;

    public IReadOnlyList<TargetingOpponent> Targeters => Volatile.Read(ref targeters);

    public TargetTrackerDiagnostics Diagnostics => Volatile.Read(ref diagnostics);

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) return;
        started = true;
        try
        {
            pressureCapture?.Start();
        }
        catch (Exception exception)
        {
            log.Error(exception, "HOWMANY could not start recent-pressure capture. Hard-target detection remains available.");
        }

        framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (started) framework.Update -= OnFrameworkUpdate;
        pressureCapture?.Dispose();
        recentPressure.Clear();
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
            pressureCapture?.SetLocalEntityId(0);
            pressureCapture?.Clear();
            recentPressure.Clear();
            Volatile.Write(ref diagnostics,
                TargetTrackerDiagnostics.Inactive(clientState.TerritoryType, pressureCapture?.IsAvailable == true));
            Publish(false, []);
            return;
        }

        var localPlayer = objectTable.LocalPlayer;
        if (localPlayer is null)
        {
            pressureCapture?.SetLocalEntityId(0);
            recentPressure.Clear();
            Volatile.Write(ref diagnostics,
                TargetTrackerDiagnostics.Inactive(clientState.TerritoryType, pressureCapture?.IsAvailable == true));
            Publish(false, []);
            return;
        }

        var now = Environment.TickCount64;
        var localEntityId = unchecked((uint)localPlayer.GameObjectId);
        pressureCapture?.SetLocalEntityId(localEntityId);

        var observations = new List<PlayerObservation>();
        var hostilePlayers = 0;
        var nativeMatches = 0;
        var castMatches = 0;
        foreach (var battleCharacter in objectTable.PlayerObjects)
        {
            if (battleCharacter is not IPlayerCharacter player) continue;
            if (player.GameObjectId == localPlayer.GameObjectId) continue;

            var hostile = (player.StatusFlags & StatusFlags.Hostile) != 0;
            if (hostile) hostilePlayers++;

            var hardTargetEntityId = GetNativeHardTargetEntityId(player);
            var hardTargetMatches = hardTargetEntityId == localEntityId;
            if (hardTargetMatches) nativeMatches++;

            var castTargetMatches = player.IsCasting &&
                                    unchecked((uint)player.CastTargetObjectId) == localEntityId;
            if (castTargetMatches) castMatches++;

            observations.Add(new PlayerObservation(
                player.GameObjectId,
                hardTargetMatches || castTargetMatches ? localPlayer.GameObjectId : 0,
                player.ClassJob.IsValid ? player.ClassJob.RowId : 0,
                hostile,
                player.IsDead));
        }

        DrainPressureEvents(now, localPlayer.GameObjectId);
        RemoveExpiredPressure(now);

        var pressureSources = recentPressure.Keys.ToHashSet();
        var snapshot = TargetingSnapshot.Build(localPlayer.GameObjectId, observations, pressureSources);
        var visiblePressurePlayers = observations.Count(player =>
            !player.IsDead && pressureSources.Contains(player.GameObjectId));

        Volatile.Write(ref diagnostics, new TargetTrackerDiagnostics(
            true,
            clientState.TerritoryType,
            observations.Count,
            hostilePlayers,
            nativeMatches,
            castMatches,
            visiblePressurePlayers,
            snapshot.Count,
            pressureCapture?.IsAvailable == true));
        Publish(true, snapshot.Opponents.ToArray());
    }

    private void DrainPressureEvents(long now, ulong localGameObjectId)
    {
        if (pressureCapture is null) return;

        var windowMilliseconds = (long)(Math.Clamp(configuration.PressureWindowSeconds, 0.5f, 8f) * 1000f);
        while (pressureCapture.TryDequeue(out var pressureEvent))
        {
            var expiresAt = pressureEvent.Timestamp + windowMilliseconds;
            if (expiresAt <= now) continue;

            var sourceObject = objectTable.SearchByEntityId(pressureEvent.CasterEntityId);
            var sourcePlayer = ResolvePlayerOwner(sourceObject);
            if (sourcePlayer is null || sourcePlayer.GameObjectId == localGameObjectId || sourcePlayer.IsDead) continue;

            if (!recentPressure.TryGetValue(sourcePlayer.GameObjectId, out var currentExpiry) || expiresAt > currentExpiry)
            {
                recentPressure[sourcePlayer.GameObjectId] = expiresAt;
            }
        }
    }

    private IPlayerCharacter? ResolvePlayerOwner(IGameObject? sourceObject)
    {
        if (sourceObject is IPlayerCharacter player) return player;
        if (sourceObject is null || sourceObject.OwnerId == 0) return null;
        return objectTable.SearchByEntityId(unchecked((uint)sourceObject.OwnerId)) as IPlayerCharacter;
    }

    private void RemoveExpiredPressure(long now)
    {
        foreach (var gameObjectId in recentPressure
                     .Where(pair => pair.Value <= now)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            recentPressure.Remove(gameObjectId);
        }
    }

    private static unsafe uint GetNativeHardTargetEntityId(IPlayerCharacter player)
    {
        if (player.Address == nint.Zero) return 0;
        var character = (Character*)player.Address;
        return character->GetTargetId().ObjectId;
    }

    private void Publish(bool isActive, TargetingOpponent[] value)
    {
        Interlocked.Exchange(ref targeters, value);
        Volatile.Write(ref active, isActive ? 1 : 0);
    }
}
