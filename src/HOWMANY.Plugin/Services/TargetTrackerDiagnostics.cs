namespace HowMany.Plugin.Services;

internal sealed record TargetTrackerDiagnostics(
    bool IsActive,
    uint TerritoryId,
    int VisiblePlayers,
    int HostileFlaggedPlayers,
    int NativeHardTargetMatches,
    int CastTargetMatches,
    int RecentPressurePlayers,
    int DisplayedPlayers,
    bool PressureCaptureAvailable)
{
    public static TargetTrackerDiagnostics Inactive(uint territoryId, bool pressureCaptureAvailable) =>
        new(false, territoryId, 0, 0, 0, 0, 0, 0, pressureCaptureAvailable);

    public string ToChatLine() =>
        $"active={IsActive}, territory={TerritoryId}, visible={VisiblePlayers}, hostileFlag={HostileFlaggedPlayers}, " +
        $"hardTarget={NativeHardTargetMatches}, castTarget={CastTargetMatches}, recentPressure={RecentPressurePlayers}, " +
        $"shown={DisplayedPlayers}, pressureHook={(PressureCaptureAvailable ? "ready" : "unavailable")}";
}
