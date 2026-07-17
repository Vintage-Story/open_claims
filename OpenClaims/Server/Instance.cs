using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace OpenClaims.Server;

public class Instance
{
    internal static ICoreServerAPI api = null!;
    internal static IServerNetworkChannel serverChannel = null!;

    private readonly Dictionary<string, long> sessionStartTicks = new();
    private Dictionary<string, long> totalPlaySeconds = new();
    private readonly Commands commands;

    internal Instance()
    {
        commands = new Commands(this);
    }

    internal void Init(ICoreServerAPI serverAPI)
    {
        api = serverAPI;

        serverChannel = api.Network
            .RegisterChannel("openclaims")
            .RegisterMessageType<ClaimRequestPacket>()
            .RegisterMessageType<ClaimResizePacket>()
            .RegisterMessageType<ClaimRenamePacket>()
            .RegisterMessageType<ClaimDeletePacket>()
            .RegisterMessageType<ClaimAllowPacket>()
            .RegisterMessageType<ClaimUnallowPacket>()
            .RegisterMessageType<ClaimResponsePacket>()
            .SetMessageHandler<ClaimRequestPacket>(OnClaimRequest)
            .SetMessageHandler<ClaimResizePacket>(OnClaimResize)
            .SetMessageHandler<ClaimRenamePacket>(OnClaimRename)
            .SetMessageHandler<ClaimDeletePacket>(OnClaimDelete)
            .SetMessageHandler<ClaimAllowPacket>(OnClaimAllow)
            .SetMessageHandler<ClaimUnallowPacket>(OnClaimUnallow);

        Configuration.Load(api);

        PatchClaimAdd();

        api.Event.PlayerNowPlaying += OnPlayerJoin;
        api.Event.PlayerLeave += OnPlayerLeave;
        api.Event.RegisterGameTickListener(_ => OnProgressionTick(), 300_000);
        api.Event.SaveGameLoaded += OnSaveLoaded;
        api.Event.GameWorldSave += OnWorldSave;

        commands.Init();
    }

    // ── Claim handlers ────────────────────────────────────────────────────────

    private void OnClaimRequest(IServerPlayer player, ClaimRequestPacket p)
    {
        int mapSizeY = api.World.BlockAccessor.MapSizeY;
        var area = new Cuboidi(new BlockPos(p.MinX, 0, p.MinZ), new BlockPos(p.MaxX + 1, mapSizeY, p.MaxZ + 1));

        var allClaims = api.World.Claims.All;
        var playerClaims = allClaims.Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (!ValidateNewArea(player, area, playerClaims, allClaims, skipClaimIndex: -1)) return;

        var claim = LandClaim.CreateClaim(player, player.Role.PrivilegeLevel);
        claim.Description = $"{player.PlayerName} Claims";
        claim.Areas.Add(area);
        api.World.Claims.Add(claim);
        ReplyToPanel(player, Lang.Get("openclaims:claim_success"), success: true);
    }

    private void OnClaimResize(IServerPlayer player, ClaimResizePacket p)
    {
        int mapSizeY = api.World.BlockAccessor.MapSizeY;
        var area = new Cuboidi(new BlockPos(p.MinX, 0, p.MinZ), new BlockPos(p.MaxX + 1, mapSizeY, p.MaxZ + 1));

        var allClaims = api.World.Claims.All;
        var playerClaims = allClaims.Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (p.ClaimIndex < 0 || p.ClaimIndex >= playerClaims.Count)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_not_found"), success: false); return;
        }

        if (!ValidateNewArea(player, area, playerClaims, allClaims, skipClaimIndex: p.ClaimIndex)) return;

        var old = playerClaims[p.ClaimIndex];
        var updated = LandClaim.CreateClaim(player, old.ProtectionLevel);
        updated.Description = old.Description;
        updated.AllowUseEveryone = old.AllowUseEveryone;
        updated.AllowTraverseEveryone = old.AllowTraverseEveryone;
        updated.PermittedPlayerUids = old.PermittedPlayerUids;
        updated.PermittedPlayerGroupIds = old.PermittedPlayerGroupIds;
        updated.PermittedPlayerLastKnownPlayerName = old.PermittedPlayerLastKnownPlayerName;
        updated.Areas.Add(area);
        UpdateClaimKeepOrder(old, updated);
        ReplyToPanel(player, Lang.Get("openclaims:resize_success"), success: true);
    }

    private void OnClaimRename(IServerPlayer player, ClaimRenamePacket p)
    {
        var playerClaims = api.World.Claims.All
            .Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (p.ClaimIndex < 0 || p.ClaimIndex >= playerClaims.Count)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_not_found"), success: false); return;
        }

        playerClaims[p.ClaimIndex].Description = p.NewName;
        BroadcastAllClaims();
        ReplyToPanel(player, Lang.Get("openclaims:rename_success"), success: true);
    }

    private void OnClaimAllow(IServerPlayer player, ClaimAllowPacket p)
    {
        var playerClaims = api.World.Claims.All
            .Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (p.ClaimIndex < 0 || p.ClaimIndex >= playerClaims.Count)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_not_found"), success: false); return;
        }

        var target = api.World.AllOnlinePlayers
            .FirstOrDefault(pl => pl.PlayerName.Equals(p.PlayerName, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_player_offline", p.PlayerName), success: false); return;
        }

        if (target.PlayerUID == player.PlayerUID)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_allow_self"), success: false); return;
        }

        var claim = playerClaims[p.ClaimIndex];
        if (!claim.PermittedPlayerUids.ContainsKey(target.PlayerUID))
        {
            claim.PermittedPlayerUids[target.PlayerUID] = EnumBlockAccessFlags.BuildOrBreak | EnumBlockAccessFlags.Use;
            claim.PermittedPlayerLastKnownPlayerName[target.PlayerUID] = target.PlayerName;
            BroadcastAllClaims();
        }
        ReplyToPanel(player, Lang.Get("openclaims:allow_success", target.PlayerName), success: true);
    }

    private void OnClaimUnallow(IServerPlayer player, ClaimUnallowPacket p)
    {
        var playerClaims = api.World.Claims.All
            .Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (p.ClaimIndex < 0 || p.ClaimIndex >= playerClaims.Count)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_not_found"), success: false); return;
        }

        var claim = playerClaims[p.ClaimIndex];
        if (!claim.PermittedPlayerUids.ContainsKey(p.PlayerUID))
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_player_not_allowed"), success: false); return;
        }

        claim.PermittedPlayerLastKnownPlayerName.TryGetValue(p.PlayerUID, out string? name);
        claim.PermittedPlayerUids.Remove(p.PlayerUID);
        claim.PermittedPlayerLastKnownPlayerName.Remove(p.PlayerUID);
        BroadcastAllClaims();
        ReplyToPanel(player, Lang.Get("openclaims:unallow_success", name ?? p.PlayerUID), success: true);
    }

    private void OnClaimDelete(IServerPlayer player, ClaimDeletePacket p)
    {
        var playerClaims = api.World.Claims.All
            .Where(c => c.OwnedByPlayerUid == player.PlayerUID).ToList();

        if (p.ClaimIndex < 0 || p.ClaimIndex >= playerClaims.Count)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_not_found"), success: false); return;
        }

        api.World.Claims.Remove(playerClaims[p.ClaimIndex]);
        ReplyToPanel(player, Lang.Get("openclaims:delete_success"), success: true);
    }

    private void UpdateClaimKeepOrder(LandClaim old, LandClaim updated)
    {
        var claims = api.World.Claims;
        var all = claims.All;
        int originalIndex = all.IndexOf(old);

        claims.Remove(old);
        claims.Add(updated);

        if (originalIndex >= 0 && originalIndex < all.Count)
        {
            all.Remove(updated);
            all.Insert(originalIndex, updated);
        }

        BroadcastAllClaims();
    }

    internal static void BroadcastAllClaims()
    {
        var claims = api.World.Claims;
        claims.GetType()
            .GetMethod("BroadcastClaims", BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(claims, new object?[] { claims.All, null });
    }

    private bool ValidateNewArea(IServerPlayer player, Cuboidi area,
        List<LandClaim> playerClaims, List<LandClaim> allClaims, int skipClaimIndex)
    {
        int maxAreas = player.Role.LandClaimMaxAreas + player.ServerData.ExtraLandClaimAreas;
        int effectiveCount = playerClaims.Count - (skipClaimIndex >= 0 ? 1 : 0);
        if (effectiveCount >= maxAreas)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_max_areas", maxAreas), success: false); return false;
        }

        var minSize = player.Role.LandClaimMinSize;
        if (area.SizeX < minSize.X || area.SizeZ < minSize.Z)
        {
            ReplyToPanel(player, Lang.Get("openclaims:err_too_small", area.SizeX, area.SizeZ, minSize.X, minSize.Z), success: false); return false;
        }

        int usedVol = playerClaims
            .Where((_, i) => i != skipClaimIndex)
            .Sum(c => c.SizeXYZ);
        int maxAllowance = player.Role.LandClaimAllowance + player.ServerData.ExtraLandClaimAllowance;
        if (usedVol + area.SizeXYZ > maxAllowance)
        {
            int mapH = api.World.BlockAccessor.MapSizeY;
            ReplyToPanel(player, Lang.Get("openclaims:err_allowance",
                area.SizeXYZ / mapH,
                (maxAllowance - usedVol) / mapH,
                maxAllowance / mapH), success: false); return false;
        }

        var skipClaim = skipClaimIndex >= 0 ? playerClaims[skipClaimIndex] : null;
        for (int i = 0; i < allClaims.Count; i++)
        {
            if (allClaims[i] == skipClaim) continue;
            if (allClaims[i].Intersects(area))
            {
                ReplyToPanel(player, Lang.Get("openclaims:err_overlap", allClaims[i].LastKnownOwnerName), success: false); return false;
            }
        }

        return true;
    }

    internal static void ReplyToPanel(IServerPlayer player, string msg, bool success) =>
        serverChannel.SendPacket(new ClaimResponsePacket { Message = msg, Success = success }, player);

    // ── Progressão por tempo de jogo ──────────────────────────────────────────

    internal void OnPlayerJoin(IServerPlayer player)
    {
        sessionStartTicks[player.PlayerUID] = DateTime.UtcNow.Ticks;
        ApplyProgression(player);
    }

    internal void OnPlayerLeave(IServerPlayer player)
    {
        FlushSession(player);
        sessionStartTicks.Remove(player.PlayerUID);
    }

    internal void FlushSession(IServerPlayer player)
    {
        if (!sessionStartTicks.TryGetValue(player.PlayerUID, out long start)) return;
        long elapsed = (DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerSecond;
        totalPlaySeconds.TryGetValue(player.PlayerUID, out long prev);
        totalPlaySeconds[player.PlayerUID] = prev + elapsed;
        sessionStartTicks[player.PlayerUID] = DateTime.UtcNow.Ticks;
    }

    private void OnProgressionTick()
    {
        foreach (IServerPlayer player in api.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            FlushSession(player);
            ApplyProgression(player);
        }
    }

    internal void ApplyProgression(IServerPlayer player)
    {
        totalPlaySeconds.TryGetValue(player.PlayerUID, out long total);
        double totalHours = total / 3600.0;

        int extraAreas = (int)(totalHours / Configuration.HoursPerExtraArea);
        if (Configuration.MaxExtraAreas > 0)
            extraAreas = Math.Min(extraAreas, Configuration.MaxExtraAreas);

        int extraSurface = (int)(totalHours * Configuration.SurfaceBlocksPerHour);
        if (Configuration.MaxExtraSurface > 0)
            extraSurface = Math.Min(extraSurface, Configuration.MaxExtraSurface);

        int mapSizeY = api.World.BlockAccessor.MapSizeY;
        player.ServerData.ExtraLandClaimAreas = extraAreas;
        player.ServerData.ExtraLandClaimAllowance = extraSurface * mapSizeY;
    }

    internal long GetTotalPlaySeconds(IServerPlayer player)
    {
        totalPlaySeconds.TryGetValue(player.PlayerUID, out long total);
        return total;
    }

    // ── Harmony patch ─────────────────────────────────────────────────────────

    private void PatchClaimAdd()
    {
        var addMethod = api.World.Claims.GetType()
            .GetMethod("Add", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(LandClaim) }, null);
        if (addMethod == null) return;

        new Harmony("openclaims.server").Patch(addMethod,
            prefix: new HarmonyMethod(
                typeof(Instance).GetMethod(nameof(NormalizeClaimHeightPrefix),
                    BindingFlags.NonPublic | BindingFlags.Static)));
    }

    private static void NormalizeClaimHeightPrefix(LandClaim claim)
    {
        if (api == null) return;
        int mapSizeY = api.World.BlockAccessor.MapSizeY;
        for (int i = 0; i < claim.Areas.Count; i++)
        {
            var a = claim.Areas[i];
            if (a.Y1 != 0 || a.Y2 != mapSizeY)
                claim.Areas[i] = new Cuboidi(a.X1, 0, a.Z1, a.X2, mapSizeY, a.Z2);
        }
    }

    // ── Persistência ──────────────────────────────────────────────────────────

    private void OnSaveLoaded()
    {
        byte[]? bytes = api.WorldManager.SaveGame.GetData("openclaims_playtime");
        if (bytes != null)
            totalPlaySeconds = JsonSerializer.Deserialize<Dictionary<string, long>>(Encoding.UTF8.GetString(bytes)) ?? new();
    }

    private void OnWorldSave()
    {
        if (api == null) return;
        foreach (IServerPlayer player in api.World.AllOnlinePlayers.Cast<IServerPlayer>())
            FlushSession(player);
        string json = JsonSerializer.Serialize(totalPlaySeconds);
        api.WorldManager.SaveGame.StoreData("openclaims_playtime", Encoding.UTF8.GetBytes(json));
    }

    internal void Dispose()
    {
        new Harmony("openclaims.server").UnpatchAll("openclaims.server");
        if (api != null)
        {
            foreach (IServerPlayer player in api.World.AllOnlinePlayers.Cast<IServerPlayer>())
                FlushSession(player);
            string json = JsonSerializer.Serialize(totalPlaySeconds);
            api.WorldManager.SaveGame.StoreData("openclaims_playtime", Encoding.UTF8.GetBytes(json));
        }
        api = null!;
    }
}
