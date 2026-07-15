using HowMany.Core;

var tests = new (string Name, Action Run)[]
{
    ("filters to living hostile hard-targeters", FiltersTargeters),
    ("deduplicates game objects but preserves separate same-job enemies", DeduplicatesObjects),
    ("handles missing local player", HandlesMissingLocalPlayer),
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

Console.WriteLine($"All {tests.Length} HOWMANY core tests passed.");

static void FiltersTargeters()
{
    const ulong local = 100;
    PlayerObservation[] players =
    [
        new(local, local, 21, true, false),
        new(1, local, 32, true, false),
        new(2, local, 38, false, false),
        new(3, local, 40, true, true),
        new(4, 999, 19, true, false),
        new(5, local, 30, true, false),
    ];

    var snapshot = TargetingSnapshot.Build(local, players);
    Equal(2, snapshot.Count, "count");
    SequenceEqual(new ulong[] { 1, 5 }, snapshot.Opponents.Select(x => x.GameObjectId), "target order");
    SequenceEqual(new uint[] { 32, 30 }, snapshot.Opponents.Select(x => x.JobId), "job order");
}

static void DeduplicatesObjects()
{
    const ulong local = 200;
    PlayerObservation[] players =
    [
        new(10, local, 38, true, false),
        new(10, local, 38, true, false),
        new(11, local, 38, true, false),
        new(0, local, 40, true, false),
    ];

    var snapshot = TargetingSnapshot.Build(local, players);
    Equal(2, snapshot.Count, "unique targeters");
    SequenceEqual(new uint[] { 38, 38 }, snapshot.Opponents.Select(x => x.JobId), "duplicate jobs remain visible");
}

static void HandlesMissingLocalPlayer()
{
    var snapshot = TargetingSnapshot.Build(0, [new(1, 0, 32, true, false)]);
    Equal(0, snapshot.Count, "missing local player count");
}

static void Equal<T>(T expected, T actual, string label) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
}

static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string label)
{
    if (!expected.SequenceEqual(actual))
        throw new InvalidOperationException($"{label}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
}
