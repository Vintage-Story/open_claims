using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

// Bloqueia pan por botão esquerdo do GuiElementMap para que o drag esquerdo
// sempre inicie uma seleção de claim. Notifica o ClaimSelectionLayer para iniciar o drag.
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
            return false; // pula método original → IsDragingMap nunca vira true
        }

        return true;
    }
}

// GuiElementScrollbar.OnMouseWheel não checa posição do mouse — age no segundo
// passo do GuiComposer (todos os elementos) mesmo com o mouse sobre o mapa.
// Este patch restringe o nosso scrollbar específico a só responder quando o
// mouse está dentro dos bounds do painel de claims.
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

