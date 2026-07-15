using System.Collections.Concurrent;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GameObjectId = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObjectId;

namespace HowMany.Plugin.Services;

internal readonly record struct PressureEvent(uint CasterEntityId, long Timestamp);

/// <summary>
/// Captures only short-lived, harmful action attempts directed at the local player.
/// The game-thread tracker resolves the source object later; no Dalamud object APIs are
/// touched from the action-effect detour.
/// </summary>
internal sealed unsafe class ActionPressureCapture : IDisposable
{
    private const int MaximumTargetsPerAction = 32;

    private readonly ConcurrentQueue<PressureEvent> pending = new();
    private readonly Hook<ActionEffectHandler.Delegates.Receive> hook;
    private readonly IPluginLog log;
    private uint localEntityId;
    private int errorLogged;
    private bool started;
    private bool disposed;

    public ActionPressureCapture(IGameInteropProvider interop, IPluginLog log)
    {
        this.log = log;
        hook = interop.HookFromAddress<ActionEffectHandler.Delegates.Receive>(
            ActionEffectHandler.MemberFunctionPointers.Receive,
            ReceiveDetour);
    }

    public bool IsAvailable => started && !disposed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (started) return;
        hook.Enable();
        started = true;
    }

    public void SetLocalEntityId(uint value) => Volatile.Write(ref localEntityId, value);

    public bool TryDequeue(out PressureEvent value) => pending.TryDequeue(out value);

    public void Clear()
    {
        while (pending.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Volatile.Write(ref localEntityId, 0);
        Clear();
        hook.Dispose();
    }

    private void ReceiveDetour(
        uint casterEntityId,
        Character* casterPtr,
        Vector3* targetPosition,
        ActionEffectHandler.Header* header,
        ActionEffectHandler.TargetEffects* effects,
        GameObjectId* targetEntityIds)
    {
        try
        {
            var localId = Volatile.Read(ref localEntityId);
            if (localId == 0 || casterEntityId == 0 || casterEntityId == localId ||
                header == null || effects == null || targetEntityIds == null)
            {
                return;
            }

            var targetCount = Math.Min((int)header->NumTargets, MaximumTargetsPerAction);
            for (var index = 0; index < targetCount; index++)
            {
                if (targetEntityIds[index].ObjectId != localId || !HasPressureEffect(&effects[index])) continue;
                pending.Enqueue(new PressureEvent(casterEntityId, Environment.TickCount64));
                break;
            }
        }
        catch (Exception exception)
        {
            if (Interlocked.Exchange(ref errorLogged, 1) == 0)
            {
                log.Error(exception, "HOWMANY action-pressure capture failed; further hook errors are suppressed.");
            }
        }
        finally
        {
            hook.Original(casterEntityId, casterPtr, targetPosition, header, effects, targetEntityIds);
        }
    }

    private static bool HasPressureEffect(ActionEffectHandler.TargetEffects* targetEffects)
    {
        var slots = targetEffects->Effects;
        for (var index = 0; index < slots.Length; index++)
        {
            if (IsPressureEffect(slots[index].Type)) return true;
        }

        return false;
    }

    private static bool IsPressureEffect(byte type) => (PressureEffectType)type is
        PressureEffectType.Miss or
        PressureEffectType.FullResist or
        PressureEffectType.Damage or
        PressureEffectType.BlockedDamage or
        PressureEffectType.ParriedDamage or
        PressureEffectType.Invulnerable or
        PressureEffectType.PartialInvulnerable;

    private enum PressureEffectType : byte
    {
        Miss = 1,
        FullResist = 2,
        Damage = 3,
        BlockedDamage = 5,
        ParriedDamage = 6,
        Invulnerable = 7,
        PartialInvulnerable = 74,
    }
}
