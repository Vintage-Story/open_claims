using HarmonyLib;
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

    [HarmonyPrefix]
    [HarmonyPatch("OnMouseDownOnElement")]
    public static bool OnMouseDownOnElement(GuiElementMap __instance, MouseEvent args)
    {
        if (ClaimModeActive && args.Button == EnumMouseButton.Left)
        {
            ActiveLayer?.StartDrag(args, __instance);
            return false; // skip original method so IsDragingMap never becomes true
        }

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

