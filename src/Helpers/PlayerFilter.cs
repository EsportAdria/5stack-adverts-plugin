using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Permissions;

namespace SwiftlyS2.Adverts.Helpers;

public static class PlayerFilter
{
    /// <summary>
    /// Returns true if the target string is a known value.
    /// Unknown targets return false — caller should log a warning.
    /// </summary>
    public static bool IsValidTarget(string target)
    {
        return target.ToLowerInvariant() is "all" or "alive" or "dead" or "t" or "ct";
    }

    public static IEnumerable<IPlayer> FilterPlayers(
        IEnumerable<IPlayer> players,
        string target,
        List<string> skipPermissions,
        IPermissionManager permService)
    {
        // Filter out players with null Pawn (not fully connected)
        var validPlayers = players.Where(p => p.Pawn != null);

        var filtered = target.ToLowerInvariant() switch
        {
            "alive" => validPlayers.Where(p => p.Pawn!.LifeState == 0),
            "dead" => validPlayers.Where(p => p.Pawn!.LifeState != 0),
            "t" => validPlayers.Where(p => p.Pawn!.TeamNum == 2),
            "ct" => validPlayers.Where(p => p.Pawn!.TeamNum == 3),
            _ => validPlayers // "all" or unknown defaults to all
        };

        // Exclude players with skip permissions
        if (skipPermissions.Count > 0)
        {
            filtered = filtered.Where(p =>
                !skipPermissions.Any(perm =>
                    permService.PlayerHasPermissions(p.SteamID, new[] { perm })));
        }

        return filtered;
    }
}
