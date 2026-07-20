using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer
{
    private void BuildClaimsPanel(GuiDialogWorldMap dlg)
    {
        const double PanelW  = 270.0;
        const double ScrollW = 20.0;
        const double ListH   = 175.0;
        const double BtnH    = 25.0;
        const double LineH   = 18.0;
        const double CharW   = 7.0; // approximate average char width in WhiteSmallText

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithFixedPosition(panelFixedX, panelFixedY)
            .WithAlignment(EnumDialogArea.None);

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var insetBounds = ElementBounds.Fixed(0, 30, PanelW - ScrollW, ListH);
        var clipBounds  = insetBounds.ForkContainingChild(5, 5, 5, 5);
        var rtBounds    = ElementBounds.Fixed(0, 0, clipBounds.fixedWidth - 5, 2000);
        var btnBounds      = ElementBounds.Fixed(0, 0, PanelW, BtnH).FixedUnder(insetBounds, 8);
        var viewBtnBounds  = ElementBounds.Fixed(0, 0, PanelW, BtnH).FixedUnder(btnBounds, 4);

        // pre-compute height required for the status text
        int statusLines = string.IsNullOrEmpty(LastStatusMessage)
            ? 0
            : Math.Max(1, (int)Math.Ceiling(LastStatusMessage.Length * CharW / (PanelW - 10))) + 1;
        double statusH = statusLines * LineH;

        string btnText = claimModeActive ? Lang.Get("openclaims:btn_disable") : Lang.Get("openclaims:btn_enable");

        var composer = capi.Gui.CreateCompo(PanelKey, dialogBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("openclaims:panel_title"), () => dlg.Composers[PanelKey].Enabled = false)
            .BeginChildElements(bgBounds)
            .AddInset(insetBounds)
            .AddVerticalScrollbar(OnClaimScrollbarChanged, ElementStdBounds.VerticalScrollbar(insetBounds), "claimScrollbar")
            .BeginClip(clipBounds)
            .AddRichtext(BuildClaimsVtml(), CairoFont.WhiteSmallText(), rtBounds, "claimList")
            .EndClip()
            .AddSmallButton(btnText, OnToggleClaimMode, btnBounds, EnumButtonStyle.Normal, "btnToggle")
            .AddSmallButton(viewClaimsActive ? Lang.Get("openclaims:btn_hide_claims") : Lang.Get("openclaims:btn_view_claims"), OnViewClaims, viewBtnBounds, EnumButtonStyle.Normal, "btnViewClaims");

        if (statusLines > 0)
        {
            var statusFont   = LastStatusSuccess
                ? CairoFont.WhiteSmallText().WithColor(new double[] { 0.3, 1.0, 0.4, 1.0 })
                : CairoFont.WhiteSmallText().WithColor(new double[] { 1.0, 0.3, 0.25, 1.0 });
            var statusBounds = ElementBounds.Fixed(0, 0, PanelW, statusH).FixedUnder(viewBtnBounds, 4);
            composer.AddStaticText(LastStatusMessage, statusFont, statusBounds);
        }

        dlg.Composers[PanelKey] = composer.EndChildElements().Compose();

        dlg.Composers[PanelKey].Enabled = false;
        PanelComposer = dlg.Composers[PanelKey];

        double visH = clipBounds.fixedHeight;
        capi.Event.EnqueueMainThreadTask(() =>
        {
            var compo = dlg.Composers[PanelKey];
            if (compo == null) return;
            var rt = compo.GetRichtext("claimList");
            var sb = compo.GetScrollbar("claimScrollbar");
            if (rt == null || sb == null) return;
            double contentH = rt.Bounds.OuterHeight / RuntimeEnv.GUIScale;
            sb.SetHeights((float)visH, (float)Math.Max(contentH, visH));
            PanelScrollbar = sb;
        }, "InitClaimsScrollbar");
    }

    private string BuildClaimsVtml()
    {
        string myUid  = capi.World.Player.PlayerUID;
        var claims    = capi.World.Claims.All.FindAll(c => c.OwnedByPlayerUid == myUid);

        if (claims.Count == 0)
            return Lang.Get("openclaims:no_claims");

        var sb = new StringBuilder();
        for (int i = 0; i < claims.Count; i++)
        {
            var claim = claims[i];
            if (i > 0) sb.AppendLine();

            string label = !string.IsNullOrWhiteSpace(claim.Description)
                ? claim.Description
                : Lang.Get("openclaims:claim_label", i + 1);

            bool isResizing = ResizingClaimIndex == i;
            string resizeLink = isResizing
                ? $"<font color=\"#ffaa33\"><a href=\"openclaims://cancel_resize:{i}\">{Lang.Get("openclaims:link_cancel_resize")}</a></font>"
                : $"<a href=\"openclaims://resize:{i}\">{Lang.Get("openclaims:link_resize")}</a>";

            string unallowLink = claim.PermittedPlayerLastKnownPlayerName.Count > 0
                ? $" | <a href=\"openclaims://unallow:{i}\">{Lang.Get("openclaims:link_unallow")}</a>"
                : "";

            sb.AppendLine($"<font color=\"#44ff66\"><strong>{label}</strong></font>" +
                          $"  <a href=\"openclaims://rename:{i}\">{Lang.Get("openclaims:link_rename")}</a>" +
                          $" | {resizeLink}" +
                          $" | <a href=\"openclaims://allow:{i}\">{Lang.Get("openclaims:link_allow")}</a>" +
                          unallowLink +
                          $" | <a href=\"openclaims://delete:{i}\">{Lang.Get("openclaims:link_delete")}</a>");

            foreach (var area in claim.Areas)
            {
                int sX = area.MaxX - area.MinX + 1;
                int sZ = area.MaxZ - area.MinZ + 1;
                var spawn = capi.World.DefaultSpawnPosition;
                int cx = (area.MinX + area.MaxX) / 2 - (int)spawn.X;
                int cz = (area.MinZ + area.MaxZ) / 2 - (int)spawn.Z;
                sb.AppendLine($"X: {cx}, Z: {cz}");
                sb.Append(Lang.Get("openclaims:claim_size", sX, sZ));
            }
        }
        return sb.ToString();
    }

    private void OnClaimLinkClicked(LinkTextComponent comp)
    {
        // href = "openclaims://action:index" or "openclaims://action:index:extra"
        string path  = comp.Href.Substring("openclaims://".Length);
        var parts    = path.Split(':');
        if (parts.Length < 2 || !int.TryParse(parts[1], out int idx)) return;

        string myUid  = capi.World.Player.PlayerUID;
        var claims    = capi.World.Claims.All.FindAll(c => c.OwnedByPlayerUid == myUid);
        if (idx < 0 || idx >= claims.Count) return;

        switch (parts[0])
        {
            case "rename":
                double rnX = panelFixedX;
                double rnY = panelFixedY + (PanelComposer?.Bounds.OuterHeight / RuntimeEnv.GUIScale ?? 280) + 5;
                new ClaimRenameDialog(capi, idx, claims[idx].Description ?? "", (i, name) =>
                {
                    OnRenameClaim?.Invoke(i, name);
                    ScheduleRefresh();
                }, rnX, rnY).TryOpen();
                break;

            case "resize":
                ResizingClaimIndex = idx;
                claimModeActive = true;
                OverwriteMapPan.ClaimModeActive = true;
                OverwriteMapPan.ActiveLayer = this;
                RefreshClaimsPanel();
                break;

            case "cancel_resize":
                ResizingClaimIndex = -1;
                claimModeActive = false;
                OverwriteMapPan.ClaimModeActive = false;
                OverwriteMapPan.ActiveLayer = null;
                ClearSelection();
                RefreshClaimsPanel();
                break;

            case "allow":
                double invX = panelFixedX;
                double invY = panelFixedY + (PanelComposer?.Bounds.OuterHeight / RuntimeEnv.GUIScale ?? 280) + 5;
                new ClaimAllowDialog(capi, idx, (i, name) =>
                {
                    OnAllowPlayer?.Invoke(i, name);
                }, invX, invY).TryOpen();
                break;

            case "delete":
                double dlgX = panelFixedX;
                double dlgY = panelFixedY + (PanelComposer?.Bounds.OuterHeight / RuntimeEnv.GUIScale ?? 280) + 5;
                new ClaimConfirmDialog(capi, Lang.Get("openclaims:confirm_delete_msg", claims[idx].Description ?? $"Claim {idx + 1}"),
                    () =>
                    {
                        OnDeleteClaim?.Invoke(idx);
                        ScheduleRefresh();
                    }, dlgX, dlgY).TryOpen();
                break;

            case "unallow":
                var permPlayers = new System.Collections.Generic.List<(string uid, string name)>();
                foreach (var kv in claims[idx].PermittedPlayerLastKnownPlayerName)
                    permPlayers.Add((kv.Key, kv.Value));
                double unX = panelFixedX;
                double unY = panelFixedY + (PanelComposer?.Bounds.OuterHeight / RuntimeEnv.GUIScale ?? 280) + 5;
                new ClaimUnallowDialog(capi, idx, permPlayers, (i2, uid) =>
                {
                    OnUnallowPlayer?.Invoke(i2, uid);
                }, unX, unY).TryOpen();
                break;
        }
    }

    private void OnClaimScrollbarChanged(float value)
    {
        if (mapDlg == null) return;
        var rt = mapDlg.Composers[PanelKey]?.GetRichtext("claimList");
        if (rt == null) return;
        rt.Bounds.fixedY = -value;
        rt.Bounds.CalcWorldBounds();
    }

    private bool OnViewClaims()
    {
        viewClaimsActive = !viewClaimsActive;

        if (viewClaimsActive)
            ApplyClaimHighlights();
        else
            ClearClaimHighlights();

        RefreshClaimsPanel();
        return true;
    }

    private void ApplyClaimHighlights()
    {
        string myUid = capi.World.Player.PlayerUID;
        var blocks = new List<BlockPos>();
        var colors = new List<int>();

        // ToRgba(a, r, g, b) stores bytes as [b, g, r, a]; OpenGL reads RGBA, so R and B are swapped.
        // To get visual color (vR, vG, vB): pass ToRgba(a, vB, vG, vR).
        int colorOwn       = ColorUtil.ToRgba(100, 100, 255, 100); // verde   (symmetric — vR=vB=100)
        int colorPermitted = ColorUtil.ToRgba(100,  50, 210, 230); // amarelo (vR=230 vG=210 vB=50)
        int colorForbidden = ColorUtil.ToRgba(100,  60,  60, 220); // vermelho (vR=220 vG=60 vB=60)

        const int MaxHighlightRadius = 512; // blocos — evita tesselar claims distantes
        var pp = capi.World.Player.Entity.Pos.AsBlockPos;

        foreach (var claim in capi.World.Claims.All.Where(c =>
            !string.IsNullOrEmpty(c.OwnedByPlayerUid) &&
            c.Areas.Any(a =>
            {
                var ctr = a.Center;
                return Math.Abs(ctr.X - pp.X) + Math.Abs(ctr.Z - pp.Z) <= MaxHighlightRadius;
            })))
        {
            int color;
            if (claim.OwnedByPlayerUid == myUid)
            {
                color = colorOwn;
            }
            else
            {
                var access = claim.TestPlayerAccess(capi.World.Player, EnumBlockAccessFlags.Use);
                bool hasAccess = access != EnumPlayerAccessResult.Denied
                              || claim.AllowUseEveryone
                              || claim.AllowTraverseEveryone;
                color = hasAccess ? colorPermitted : colorForbidden;
            }

            foreach (var area in claim.Areas)
            {
                blocks.Add(area.Start.ToBlockPos());
                blocks.Add(area.End.ToBlockPos());
                colors.Add(color);
            }
        }

        capi.World.HighlightBlocks(capi.World.Player, 4, blocks, colors,
            EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cubes);
    }

    private void ClearClaimHighlights() =>
        capi.World.HighlightBlocks(capi.World.Player, 4, new List<BlockPos>(), new List<int>());

    private bool OnToggleClaimMode()
    {
        claimModeActive = !claimModeActive;
        OverwriteMapPan.ClaimModeActive = claimModeActive;
        OverwriteMapPan.ActiveLayer = claimModeActive ? this : null;
        if (!claimModeActive)
        {
            ResizingClaimIndex = -1;
            ClearSelection();
        }
        RefreshClaimsPanel();
        return true;
    }

    public void ScheduleRefresh()
    {
        long listenerId = 0;
        listenerId = capi.Event.RegisterGameTickListener(_ =>
        {
            capi.Event.UnregisterGameTickListener(listenerId);
            RefreshClaimsPanel();
        }, 200);
    }

    public string LastStatusMessage = "";
    public bool LastStatusSuccess = true;

    public void RefreshClaimsPanel()
    {
        if (mapDlg == null) return;
        bool wasEnabled = mapDlg.Composers[PanelKey]?.Enabled ?? false;
        BuildClaimsPanel(mapDlg);
        if (mapDlg.Composers[PanelKey] != null)
            mapDlg.Composers[PanelKey].Enabled = wasEnabled;
        InvalidateRenderCache();
    }
}
