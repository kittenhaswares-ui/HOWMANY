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
        IEnumerable<PlayerObservation> players,
        IReadOnlySet<ulong>? recentPressureSources = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        if (localGameObjectId == 0) return Empty;

        var seen = new HashSet<ulong>();
        var opponents = new List<TargetingOpponent>();
        foreach (var player in players)
        {
            var hasExactHostileTarget = player.TargetObjectId == localGameObjectId && player.IsHostile;
            var hasRecentPressure = recentPressureSources?.Contains(player.GameObjectId) == true;
            if (player.GameObjectId == 0 ||
                player.GameObjectId == localGameObjectId ||
                (!hasExactHostileTarget && !hasRecentPressure) ||
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
