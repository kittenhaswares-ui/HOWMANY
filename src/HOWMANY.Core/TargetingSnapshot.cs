namespace HowMany.Core;

public readonly record struct PlayerObservation(
    ulong GameObjectId,
    ulong TargetObjectId,
    uint JobId,
    bool IsHostile,
    bool IsDead);

public readonly record struct TargetingOpponent(ulong GameObjectId, uint JobId);

public sealed class TargetingSnapshot
{
    public static TargetingSnapshot Empty { get; } = new([]);

    public TargetingSnapshot(IReadOnlyList<TargetingOpponent> opponents)
    {
        Opponents = opponents;
    }

    public IReadOnlyList<TargetingOpponent> Opponents { get; }

    public int Count => Opponents.Count;

    public static TargetingSnapshot Build(
        ulong localGameObjectId,
        IEnumerable<PlayerObservation> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (localGameObjectId == 0) return Empty;

        var seen = new HashSet<ulong>();
        var opponents = new List<TargetingOpponent>();
        foreach (var player in players)
        {
            if (player.GameObjectId == 0 ||
                player.GameObjectId == localGameObjectId ||
                player.TargetObjectId != localGameObjectId ||
                !player.IsHostile ||
                player.IsDead ||
                !seen.Add(player.GameObjectId))
            {
                continue;
            }

            opponents.Add(new TargetingOpponent(player.GameObjectId, player.JobId));
        }

        return opponents.Count == 0 ? Empty : new TargetingSnapshot(opponents.ToArray());
    }
}
