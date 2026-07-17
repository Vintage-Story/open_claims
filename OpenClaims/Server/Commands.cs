using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace OpenClaims.Server;

class Commands
{
    private readonly Instance instance;

    internal Commands(Instance instance)
    {
        this.instance = instance;
    }

    internal void Init()
    {
        Instance.api.ChatCommands.Create("claimtime")
            .WithDescription("Mostra seu tempo jogado e progressão de claims")
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnClaimTimeCommand);
    }

    private TextCommandResult OnClaimTimeCommand(TextCommandCallingArgs args)
    {
        if (args.Caller.Player is not IServerPlayer player)
            return TextCommandResult.Error(Lang.Get("openclaims:claimtime_player_only"));

        instance.FlushSession(player);
        instance.ApplyProgression(player);

        long total = instance.GetTotalPlaySeconds(player);
        double hours = total / 3600.0;

        int mapH = Instance.api.World.BlockAccessor.MapSizeY;
        int extraAreas = player.ServerData.ExtraLandClaimAreas;
        int maxAreas = player.Role.LandClaimMaxAreas + extraAreas;
        int baseSurface = player.Role.LandClaimAllowance / mapH;
        int extraSurface = player.ServerData.ExtraLandClaimAllowance / mapH;
        int totalSurface = baseSurface + extraSurface;

        return TextCommandResult.Success(Lang.Get("openclaims:claimtime_output",
            hours.ToString("F2"), maxAreas, player.Role.LandClaimMaxAreas, extraAreas,
            totalSurface, baseSurface, extraSurface));
    }
}
