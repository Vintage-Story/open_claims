using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer : MapLayer
{
    private ICoreClientAPI capi;
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

    // >= 0 enquanto o usuário está redesenhando um claim existente
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
        capi.RegisterLinkProtocol("openclaims", OnClaimLinkClicked);
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap dlg, GuiComposer compo)
    {
        mapDlg = dlg;

        panelFixedX = (compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10.0;

        double baseY = compo.Bounds.renderY / RuntimeEnv.GUIScale;
        var prospComp = dlg.Composers["worldmap-layer-prospecting"];
        if (prospComp?.Bounds != null && prospComp.Bounds.OuterHeight > 0)
            panelFixedY = prospComp.Bounds.fixedY + prospComp.Bounds.OuterHeight / RuntimeEnv.GUIScale + 10.0;
        else
            panelFixedY = baseY;

        BuildClaimsPanel(dlg);
    }

    public override void OnMapOpenedClient()
    {
        quadModel ??= capi.Render.UploadMesh(QuadMeshUtil.GetQuad());
        lineRectModel ??= capi.Render.UploadMesh(LineMeshUtil.GetRectangle(-1));

        OverwriteMapPan.ClaimModeActive = claimModeActive;
        OverwriteMapPan.ActiveLayer = claimModeActive ? this : null;
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
