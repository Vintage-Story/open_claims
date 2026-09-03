using HarmonyLib;
using OpenConfiguration;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

// Blocks left-button pan on GuiElementMap so that a left drag always starts a claim selection.
[HarmonyPatch(typeof(GuiElementMap))]
public static class OverwriteMapPan
{
    public static bool ClaimModeActive;
    public static ClaimSelectionLayer? ActiveLayer;
    public static ModLogger Logger = ModLogger.None;

    [HarmonyPrefix]
    [HarmonyPatch("OnMouseDownOnElement")]
    public static bool OnMouseDownOnElement(GuiElementMap __instance, MouseEvent args)
    {
        if (ClaimModeActive && args.Button == EnumMouseButton.Left)
        {
            ActiveLayer?.StartDrag(args, __instance);
            return false; // skip original so IsDragingMap never becomes true
        }

        // If the panel is visible and the click lands inside it, don't let the map
        // consume the event (args.Handled = true) — the dialog loop will then reach
        // the panel composer and dispatch it correctly.
        var panel = ClaimSelectionLayer.PanelComposer;
        if (panel != null && panel.Enabled && panel.Bounds.PointInside(args.X, args.Y))
            return false;

        return true;
    }
}

// GuiElementScrollbar.OnMouseWheel does not check the mouse position — it fires for all
// elements even when the mouse is over the map. This patch restricts our scrollbar to
// only respond when the mouse is inside the claims panel bounds.
[HarmonyPatch(typeof(GuiElementScrollbar), "OnMouseWheel")]
public static class OverwriteClaimsScrollbarWheel
{
    [HarmonyPrefix]
    public static bool Prefix(GuiElementScrollbar __instance, ICoreClientAPI api)
    {
        if (__instance != ClaimSelectionLayer.PanelScrollbar) return true;

        var compo = ClaimSelectionLayer.PanelComposer;
        return compo != null && compo.Enabled
               && compo.Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
    }
}

