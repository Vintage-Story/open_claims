using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer
{
    public void StartDrag(MouseEvent args, GuiElementMap mapElem)
    {
        selStart = MouseToBlock(args, mapElem);
        selEnd = selStart;
        isDragging = true;
    }

    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        var mouseBlock = MouseToBlock(args, mapElem);
        if (isDragging)
            selEnd = mouseBlock;
        AppendSelectionHint(hoverText, mouseBlock);
    }

    public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
    {
        if (!isDragging || args.Button != EnumMouseButton.Left) return;

        selEnd = MouseToBlock(args, mapElem);
        isDragging = false;

        if (selStart != null && selEnd != null)
            OnSelectionConfirmed?.Invoke(selStart, selEnd);

        ClearSelection();
    }

    public void ClearSelection()
    {
        selStart = null;
        selEnd = null;
        isDragging = false;
    }

    public (Vec2i? start, Vec2i? end) GetSelection() => (selStart, selEnd);

    private static Vec2i MouseToBlock(MouseEvent args, GuiElementMap mapElem)
    {
        double viewW = mapElem.Bounds.InnerWidth;
        double viewH = mapElem.Bounds.InnerHeight;
        double relX = args.X - mapElem.Bounds.renderX;
        double relZ = args.Y - mapElem.Bounds.renderY;
        var bv = mapElem.CurrentBlockViewBounds;
        double worldX = relX / viewW * (bv.X2 - bv.X1) + bv.X1;
        double worldZ = relZ / viewH * (bv.Z2 - bv.Z1) + bv.Z1;
        return new Vec2i((int)Math.Floor(worldX), (int)Math.Floor(worldZ));
    }

    private void AppendSelectionHint(StringBuilder hoverText, Vec2i mouseBlock)
    {
        foreach (var claim in capi.World.Claims.All)
        {
            foreach (var area in claim.Areas)
            {
                if (mouseBlock.X >= area.MinX && mouseBlock.X < area.MaxX &&
                    mouseBlock.Y >= area.MinZ && mouseBlock.Y < area.MaxZ)
                {
                    if (!string.IsNullOrWhiteSpace(claim.Description))
                        hoverText.AppendLine(claim.Description);
                    hoverText.AppendLine(Lang.Get("openclaims:claimed_by", claim.LastKnownOwnerName));
                    return;
                }
            }
        }

        if (!claimModeActive) return;

        if (selStart == null)
        {
            hoverText.AppendLine(Lang.Get("openclaims:select_hint"));
            return;
        }

        if (selEnd == null) return;

        int minBX = Math.Min(selStart.X, selEnd.X);
        int minBZ = Math.Min(selStart.Y, selEnd.Y);
        int maxBX = Math.Max(selStart.X, selEnd.X);
        int maxBZ = Math.Max(selStart.Y, selEnd.Y);
        int sizeX = maxBX - minBX + 1;
        int sizeZ = maxBZ - minBZ + 1;

        if (isDragging)
            hoverText.AppendLine(Lang.Get("openclaims:select_dragging", sizeX, sizeZ));
    }
}
