using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace OpenClaims.Server;

sealed class Commands
{
    private readonly Instance instance;

    internal Commands(Instance instance)
    {
        this.instance = instance;
    }

    internal void Init()
    {
        Instance.api.ChatCommands.Create("claimtime")
            .WithDescription("Shows your playtime and claim progression")
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnClaimTimeCommand);

        Instance.api.ChatCommands.Create("claimexpire")
            .WithDescription("Forces an immediate check for expired claims")
            .RequiresPrivilege(Privilege.root)
            .HandleWith(OnClaimExpireCommand);
    }

    private TextCommandResult OnClaimExpireCommand(TextCommandCallingArgs args)
    {
        instance.RunExpirationCheck();
        return TextCommandResult.Success(Lang.Get("openclaims:claimexpire_done"));
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
            hours.ToString("F2", CultureInfo.InvariantCulture), maxAreas, player.Role.LandClaimMaxAreas, extraAreas,
            totalSurface, baseSurface, extraSurface));
    }
}
