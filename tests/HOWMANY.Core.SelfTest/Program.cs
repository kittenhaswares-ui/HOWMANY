using HowMany.Core;

var tests = new (string Name, Action Run)[]
{
    ("filters to living hostile hard-targeters", FiltersTargeters),
    ("deduplicates game objects but preserves separate same-job enemies", DeduplicatesObjects),
    ("handles missing local player", HandlesMissingLocalPlayer),
    ("adds recent harmful pressure without trusting the hostile flag", AddsRecentPressure),
    ("deduplicates exact targets and recent pressure", DeduplicatesHybridSources),
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

static void AddsRecentPressure()
{
    const ulong local = 300;
    PlayerObservation[] players =
    [
        new(20, 999, 38, false, false),
        new(21, 999, 32, false, true),
        new(22, 999, 40, false, false),
    ];

    var snapshot = TargetingSnapshot.Build(local, players, new HashSet<ulong> { 20, 21 });
    Equal(1, snapshot.Count, "living pressure source");
    SequenceEqual(new ulong[] { 20 }, snapshot.Opponents.Select(x => x.GameObjectId), "pressure source id");
}

static void DeduplicatesHybridSources()
{
    const ulong local = 400;
    PlayerObservation[] players =
    [
        new(30, local, 38, true, false),
        new(31, local, 32, true, false),
    ];

    var snapshot = TargetingSnapshot.Build(local, players, new HashSet<ulong> { 30 });
    Equal(2, snapshot.Count, "hybrid union");
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
