using System.Linq;
using System.Reflection;
using HarmonyLib;
using OpenClaims.Client;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static OpenClaims.Debug;

namespace OpenClaims;

public class Initialization : ModSystem
{
    private ICoreClientAPI? capi;
    private WorldMapManager? mapManager;
    private ClaimSelectionLayer? selLayer;
    private IClientNetworkChannel? clientChannel;

    private Server.Instance? serverInstance;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Debug.LoadLogger(api.Logger);
        Log($"Running on Version: {Mod.Info.Version}");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        mapManager = api.ModLoader.GetModSystem<WorldMapManager>();

        new Harmony("openclaims").PatchAll(Assembly.GetExecutingAssembly());

        mapManager.RegisterMapLayer<ClaimSelectionLayer>("openclaims_selection", 1.2);

        clientChannel = api.Network
            .RegisterChannel("openclaims")
            .RegisterMessageType<ClaimRequestPacket>()
            .RegisterMessageType<ClaimResizePacket>()
            .RegisterMessageType<ClaimRenamePacket>()
            .RegisterMessageType<ClaimDeletePacket>()
            .RegisterMessageType<ClaimAllowPacket>()
            .RegisterMessageType<ClaimUnallowPacket>()
            .RegisterMessageType<ClaimResponsePacket>()
            .SetMessageHandler<ClaimResponsePacket>(OnClaimResponse);

        api.Input.RegisterHotKey("openclaims_confirm", "OpenClaims: Confirmar claim", GlKeys.Enter, HotkeyType.GUIOrOtherControls);
        api.Input.SetHotKeyHandler("openclaims_confirm", _ =>
        {
            if (!OverwriteMapPan.ClaimModeActive) return false;
            ConfirmCurrentSelection();
            return true;
        });

        api.Event.LevelFinalize += OnLevelFinalize;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverInstance = new Server.Instance();
        serverInstance.Init(api);
    }

    // ── Lado cliente ───────────────────────────────────────────────────────

    private void OnLevelFinalize()
    {
        selLayer = mapManager?.MapLayers.OfType<ClaimSelectionLayer>().FirstOrDefault();
        if (selLayer == null) return;

        selLayer.OnSelectionConfirmed = OnSelectionConfirmed;
        selLayer.OnRenameClaim        = OnRenameClaim;
        selLayer.OnAllowPlayer        = OnAllowPlayer;
        selLayer.OnUnallowPlayer      = OnUnallowPlayer;
        selLayer.OnDeleteClaim        = OnDeleteClaim;

    }

    private void OnClaimResponse(ClaimResponsePacket p)
    {
        if (selLayer == null) return;
        selLayer.LastStatusMessage = p.Message;
        selLayer.LastStatusSuccess = p.Success;
        selLayer.RefreshClaimsPanel();
    }


    private void OnSelectionConfirmed(Vec2i start, Vec2i end)
    {
        int minX = System.Math.Min(start.X, end.X);
        int minZ = System.Math.Min(start.Y, end.Y);
        int maxX = System.Math.Max(start.X, end.X);
        int maxZ = System.Math.Max(start.Y, end.Y);

        if (selLayer!.ResizingClaimIndex >= 0)
        {
            clientChannel!.SendPacket(new ClaimResizePacket
            {
                ClaimIndex = selLayer.ResizingClaimIndex,
                MinX = minX, MinZ = minZ, MaxX = maxX, MaxZ = maxZ,
            });
            selLayer.ResizingClaimIndex = -1;
        }
        else
        {
            clientChannel!.SendPacket(new ClaimRequestPacket
            {
                MinX = minX, MinZ = minZ, MaxX = maxX, MaxZ = maxZ,
            });
        }
        selLayer.ScheduleRefresh();
    }

    private void OnRenameClaim(int idx, string newName) =>
        clientChannel!.SendPacket(new ClaimRenamePacket { ClaimIndex = idx, NewName = newName });

    private void OnAllowPlayer(int idx, string playerName) =>
        clientChannel!.SendPacket(new ClaimAllowPacket { ClaimIndex = idx, PlayerName = playerName });

    private void OnUnallowPlayer(int idx, string playerUid) =>
        clientChannel!.SendPacket(new ClaimUnallowPacket { ClaimIndex = idx, PlayerUID = playerUid });

    private void OnDeleteClaim(int idx) =>
        clientChannel!.SendPacket(new ClaimDeletePacket { ClaimIndex = idx });

    private void ConfirmCurrentSelection()
    {
        if (selLayer == null || !OverwriteMapPan.ClaimModeActive) return;
        var (start, end) = selLayer.GetSelection();
        if (start == null || end == null)
        {
            capi?.ShowChatMessage("[OpenClaims] No area selected on the map.");
            return;
        }
        OnSelectionConfirmed(start, end);
        selLayer.ClearSelection();
    }

    public override void Dispose()
    {
        new Harmony("openclaims").UnpatchAll("openclaims");
        serverInstance?.Dispose();
        base.Dispose();
    }
}

public static class Debug
{
    private static ILogger? logger;

    public static void LoadLogger(ILogger _logger) => logger = _logger;

    public static void Log(string message)
        => logger?.Log(EnumLogType.Notification, $"[OpenClaims] {message}");

    public static void LogWarn(string message)
        => logger?.Log(EnumLogType.Warning, $"[OpenClaims] {message}");

    public static void LogError(string message)
        => logger?.Log(EnumLogType.Error, $"[OpenClaims] {message}");
}
