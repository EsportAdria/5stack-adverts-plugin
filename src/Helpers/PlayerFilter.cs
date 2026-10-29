using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Permissions;

namespace SwiftlyS2.Adverts.Helpers;

public static class PlayerFilter
{
    public static IEnumerable<IPlayer> FilterPlayers(
        IEnumerable<IPlayer> players,
        string target,
        List<string> skipPermissions,
        IPermissionManager permService)
    {
        // Filter out players with null Pawn (not fully connected)
        var validPlayers = players.Where(p => p.Pawn != null);

        // target is already normalized by ConfigCheck
        var filtered = target switch
        {
            "alive" => validPlayers.Where(p => p.Pawn!.LifeState == 0),
            "dead" => validPlayers.Where(p => p.Pawn!.LifeState != 0),
            "t" => validPlayers.Where(p => p.Pawn!.TeamNum == 2),
            "ct" => validPlayers.Where(p => p.Pawn!.TeamNum == 3),
            _ => validPlayers // "all"
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
