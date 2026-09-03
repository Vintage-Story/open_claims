using System;
using System.Collections.Generic;
using OpenConfiguration;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer : MapLayer
{
    private ICoreClientAPI capi;
    private readonly ModLogger logger;
    private MeshRef? quadModel;
    private MeshRef? lineRectModel;
    private Matrixf mvMat = new Matrixf();
    private Vec2f v1 = new Vec2f();
    private Vec2f v2 = new Vec2f();
    private Vec3d wPos1 = new Vec3d();
    private Vec3d wPos2 = new Vec3d();

    private Vec2i? selStart;
    private Vec2i? selEnd;
    private bool isDragging;

    private GuiDialogWorldMap? mapDlg;

    // Static references used by the OverwriteClaimsScrollbarWheel patch
    public static GuiComposer?       PanelComposer;
    public static GuiElementScrollbar? PanelScrollbar;
    private double panelFixedX, panelFixedY;
    private bool claimModeActive;
    private bool viewClaimsActive;


    // >= 0 while the user is resizing an existing claim
    public int ResizingClaimIndex { get; set; } = -1;

    public Action<int>? OnDeleteClaim;
    public Action<int, string>? OnRenameClaim;
    public Action<int, string>? OnAllowPlayer;
    public Action<int, string>? OnUnallowPlayer;

    private string PanelKey => "worldmap-layer-" + LayerGroupCode;

    public Action<Vec2i, Vec2i>? OnSelectionConfirmed;

    public override string Title => "OpenClaims";
    public override string LayerGroupCode => "openclaims";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override bool RequireChunkLoaded => false;

    public ClaimSelectionLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        capi = (ICoreClientAPI)api;
        logger = new ModLogger(capi.Logger, "OpenClaims");
        capi.RegisterLinkProtocol("openclaims", OnClaimLinkClicked);
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap dlg, GuiComposer compo)
    {
        mapDlg = dlg;

        const double PanelW = 270.0;
        double screenW = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        double idealX  = (compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10.0;

        panelFixedX = (idealX + PanelW <= screenW)
            ? idealX
            : (compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale - PanelW - 10.0;

        double baseY = compo.Bounds.renderY / RuntimeEnv.GUIScale;
        var prospComp = dlg.Composers["worldmap-layer-prospecting"];
        if (prospComp?.Bounds != null && prospComp.Bounds.OuterHeight > 0)
            panelFixedY = prospComp.Bounds.fixedY + prospComp.Bounds.OuterHeight / RuntimeEnv.GUIScale + 10.0;
        else
            panelFixedY = baseY;

        BuildClaimsPanel();
    }

    public override void OnMapOpenedClient()
    {
        quadModel ??= capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
        lineRectModel ??= capi.Render.UploadMesh(LineMeshUtil.GetRectangle(-1));

        OverwriteMapPan.ClaimModeActive = claimModeActive;
        OverwriteMapPan.ActiveLayer = claimModeActive ? this : null;

        // ComposeDialogExtras inserts the panel BEFORE "single" in the Composers dictionary,
        // so it renders behind the map. Re-inserting here (after "single" is set) fixes the order.
        if (mapDlg != null)
        {
            bool wasEnabled = mapDlg.Composers[PanelKey]?.Enabled ?? false;
            mapDlg.Composers.Remove(PanelKey);
            BuildClaimsPanel();
            if (mapDlg.Composers[PanelKey] != null)
                mapDlg.Composers[PanelKey].Enabled = wasEnabled;
        }
    }

    public override void OnMapClosedClient()
    {
        claimModeActive = false;
        OverwriteMapPan.ClaimModeActive = false;
        OverwriteMapPan.ActiveLayer = null;
        PanelComposer  = null;
        PanelScrollbar = null;
        ClearSelection();
    }

    public override void Dispose()
    {
        quadModel?.Dispose();
        quadModel = null;
        lineRectModel?.Dispose();
        lineRectModel = null;
    }
}
