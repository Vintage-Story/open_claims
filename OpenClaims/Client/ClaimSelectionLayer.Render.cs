using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer
{
    public override void Render(GuiElementMap mapElem, float dt)
    {
        if (quadModel == null || lineRectModel == null) return;

        IShaderProgram sh = capi.Render.GetEngineShader((EnumShaderProgram)17);
        sh.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
        sh.Uniform("extraGlow", 0);
        sh.Uniform("applyColor", 0);
        sh.Uniform("noTexture", 1f);
        capi.Render.GlToggleBlend(true, (EnumBlendMode)0);

        string myUid = capi.World.Player.PlayerUID;
        var myClaims = capi.World.Claims.All.FindAll(c => c.OwnedByPlayerUid == myUid);
        var resizingClaim = ResizingClaimIndex >= 0 && ResizingClaimIndex < myClaims.Count
            ? myClaims[ResizingClaimIndex] : null;

        foreach (var claim in capi.World.Claims.All.Where(c => !string.IsNullOrEmpty(c.OwnedByPlayerUid)))
        {
            Vec4f fill, border;
            if (claim == resizingClaim)
            {
                // azul — claim sendo redimensionado
                fill   = new Vec4f(0.2f, 0.5f, 1.0f, 0.25f);
                border = new Vec4f(0.4f, 0.7f, 1.0f, 0.90f);
            }
            else if (claim.OwnedByPlayerUid == myUid)
            {
                // verde — claim próprio
                fill   = new Vec4f(0.1f, 1.0f, 0.3f, 0.18f);
                border = new Vec4f(0.2f, 1.0f, 0.4f, 0.85f);
            }
            else
            {
                var access = claim.TestPlayerAccess(capi.World.Player, EnumBlockAccessFlags.Use);
                bool hasAccess = access != EnumPlayerAccessResult.Denied
                              || claim.AllowUseEveryone
                              || claim.AllowTraverseEveryone;
                if (hasAccess)
                {
                    // amarelo — tem acesso mas não é dono
                    fill   = new Vec4f(1.0f, 0.9f, 0.1f, 0.18f);
                    border = new Vec4f(1.0f, 0.85f, 0.2f, 0.85f);
                }
                else
                {
                    // vermelho — sem acesso
                    fill   = new Vec4f(1.0f, 0.15f, 0.1f, 0.18f);
                    border = new Vec4f(1.0f, 0.25f, 0.2f, 0.85f);
                }
            }

            foreach (var area in claim.Areas)
            {
                var areaStart = new Vec2i(area.MinX, area.MinZ);
                var areaEnd   = new Vec2i(area.MaxX, area.MaxZ);
                RenderRect(mapElem, sh, areaStart, areaEnd, fill, border);
            }
        }

        if (selStart != null && selEnd != null)
        {
            int selMinX = Math.Min(selStart.X, selEnd.X);
            int selMinZ = Math.Min(selStart.Y, selEnd.Y);
            int selMaxX = Math.Max(selStart.X, selEnd.X);
            int selMaxZ = Math.Max(selStart.Y, selEnd.Y);
            RenderRect(mapElem, sh,
                new Vec2i(selMinX, selMinZ),
                new Vec2i(selMaxX + 1, selMaxZ + 1),
                fill:   new Vec4f(0.3f, 0.6f, 1.0f, 0.25f),
                border: new Vec4f(0.5f, 0.8f, 1.0f, 0.90f));
        }
    }

    private void RenderRect(GuiElementMap mapElem, IShaderProgram sh,
                            Vec2i blockStart, Vec2i blockEnd,
                            Vec4f fill, Vec4f border)
    {
        int minBX = Math.Min(blockStart.X, blockEnd.X);
        int minBZ = Math.Min(blockStart.Y, blockEnd.Y);
        int maxBX = Math.Max(blockStart.X, blockEnd.X);
        int maxBZ = Math.Max(blockStart.Y, blockEnd.Y);

        wPos1.Set(minBX, 0, minBZ);
        wPos2.Set(maxBX, 0, maxBZ);
        mapElem.TranslateWorldPosToViewPos(wPos1, ref v1);
        mapElem.TranslateWorldPosToViewPos(wPos2, ref v2);

        double rx = mapElem.Bounds.renderX;
        double ry = mapElem.Bounds.renderY;

        float cx = (float)(rx + (Math.Min(v1.X, v2.X) + Math.Max(v1.X, v2.X)) * 0.5);
        float cy = (float)(ry + (Math.Min(v1.Y, v2.Y) + Math.Max(v1.Y, v2.Y)) * 0.5);
        float hw = (Math.Max(v1.X, v2.X) - Math.Min(v1.X, v2.X)) * 0.5f;
        float hh = (Math.Max(v1.Y, v2.Y) - Math.Min(v1.Y, v2.Y)) * 0.5f;

        sh.Uniform("rgbaIn", fill);
        mvMat.Set(capi.Render.CurrentModelviewMatrix).Translate(cx, cy, 50f).Scale(hw, hh, 0f);
        sh.UniformMatrix("modelViewMatrix", mvMat.Values);
        capi.Render.RenderMesh(quadModel);

        sh.Uniform("rgbaIn", border);
        mvMat.Set(capi.Render.CurrentModelviewMatrix).Translate(cx, cy, 51f).Scale(hw, hh, 0f);
        sh.UniformMatrix("modelViewMatrix", mvMat.Values);
        capi.Render.RenderMesh(lineRectModel);
    }
}
