using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace OpenClaims.Client;

public partial class ClaimSelectionLayer
{
    private struct ClaimRenderEntry
    {
        public Vec2i Start, End;
        public Vec4f Fill, Border;
    }

    private volatile List<ClaimRenderEntry> renderCache = new();
    private volatile bool rebuildPending = false;
    private int cachedClaimCount = -1;
    private int cachedResizingIndex = -2;

    public override void Render(GuiElementMap mapElem, float dt)
    {
        if (quadModel == null || lineRectModel == null) return;

        TriggerCacheRebuildIfNeeded();

        IShaderProgram sh = capi.Render.GetEngineShader((EnumShaderProgram)17);
        sh.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
        sh.Uniform("extraGlow", 0);
        sh.Uniform("applyColor", 0);
        sh.Uniform("noTexture", 1f);
        capi.Render.GlToggleBlend(true, (EnumBlendMode)0);

        double mapX = mapElem.Bounds.renderX;
        double mapY = mapElem.Bounds.renderY;
        double mapW = mapElem.Bounds.OuterWidth;
        double mapH = mapElem.Bounds.OuterHeight;

        var cache = renderCache; // local ref — safe even if background swaps it mid-frame
        foreach (var entry in cache)
        {
            wPos1.Set(entry.Start.X, 0, entry.Start.Y);
            wPos2.Set(entry.End.X,   0, entry.End.Y);
            mapElem.TranslateWorldPosToViewPos(wPos1, ref v1);
            mapElem.TranslateWorldPosToViewPos(wPos2, ref v2);

            float minVX = (float)Math.Min(v1.X, v2.X);
            float maxVX = (float)Math.Max(v1.X, v2.X);
            float minVY = (float)Math.Min(v1.Y, v2.Y);
            float maxVY = (float)Math.Max(v1.Y, v2.Y);

            // viewport cull — entirely outside map view
            if (maxVX < 0 || minVX > mapW || maxVY < 0 || minVY > mapH) continue;

            float pw = maxVX - minVX;
            float ph = maxVY - minVY;

            float cx = (float)(mapX + (minVX + maxVX) * 0.5);
            float cy = (float)(mapY + (minVY + maxVY) * 0.5);
            float hw = pw * 0.5f;
            float hh = ph * 0.5f;

            sh.Uniform("rgbaIn", entry.Fill);
            mvMat.Set(capi.Render.CurrentModelviewMatrix).Translate(cx, cy, 50f).Scale(hw, hh, 0f);
            sh.UniformMatrix("modelViewMatrix", mvMat.Values);
            capi.Render.RenderMesh(quadModel);

            sh.Uniform("rgbaIn", entry.Border);
            mvMat.Set(capi.Render.CurrentModelviewMatrix).Translate(cx, cy, 51f).Scale(hw, hh, 0f);
            sh.UniformMatrix("modelViewMatrix", mvMat.Values);
            capi.Render.RenderMesh(lineRectModel);
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

    public void InvalidateRenderCache() => cachedClaimCount = -1;

    private void TriggerCacheRebuildIfNeeded()
    {
        int currentCount = capi.World.Claims.All.Count;
        if (currentCount == cachedClaimCount && ResizingClaimIndex == cachedResizingIndex) return;
        if (rebuildPending) return;

        cachedClaimCount    = currentCount;
        cachedResizingIndex = ResizingClaimIndex;
        rebuildPending      = true;

        // snapshot on main thread (safe), then compute colors in background
        var snapshot      = capi.World.Claims.All.ToList();
        var myUid         = capi.World.Player.PlayerUID;
        var player        = capi.World.Player;
        var resizingIndex = ResizingClaimIndex;

        Task.Run(() =>
        {
            var newCache = BuildCache(snapshot, myUid, player, resizingIndex);
            capi.Event.EnqueueMainThreadTask(() =>
            {
                renderCache    = newCache;
                rebuildPending = false;
            }, "openclaims_rebuildcache");
        });
    }

    private static List<ClaimRenderEntry> BuildCache(
        List<LandClaim> claims, string myUid, IPlayer player, int resizingIndex)
    {
        var myClaims = claims.FindAll(c => c.OwnedByPlayerUid == myUid);
        var resizing = resizingIndex >= 0 && resizingIndex < myClaims.Count
                       ? myClaims[resizingIndex] : null;

        var result = new List<ClaimRenderEntry>(claims.Count);

        foreach (var claim in claims.Where(c => !string.IsNullOrEmpty(c.OwnedByPlayerUid)))
        {
            Vec4f fill, border;
            if (claim == resizing)
            {
                fill   = new Vec4f(0.2f, 0.5f, 1.0f, 0.25f);
                border = new Vec4f(0.4f, 0.7f, 1.0f, 0.90f);
            }
            else if (claim.OwnedByPlayerUid == myUid)
            {
                fill   = new Vec4f(0.1f, 1.0f, 0.3f, 0.18f);
                border = new Vec4f(0.2f, 1.0f, 0.4f, 0.85f);
            }
            else
            {
                var access = claim.TestPlayerAccess(player, EnumBlockAccessFlags.Use);
                bool hasAccess = access != EnumPlayerAccessResult.Denied
                              || claim.AllowUseEveryone
                              || claim.AllowTraverseEveryone;
                fill   = hasAccess ? new Vec4f(1.0f, 0.9f, 0.1f, 0.18f) : new Vec4f(1.0f, 0.15f, 0.1f, 0.18f);
                border = hasAccess ? new Vec4f(1.0f, 0.85f, 0.2f, 0.85f) : new Vec4f(1.0f, 0.25f, 0.2f, 0.85f);
            }

            foreach (var area in claim.Areas)
                result.Add(new ClaimRenderEntry
                {
                    Start  = new Vec2i(area.MinX, area.MinZ),
                    End    = new Vec2i(area.MaxX, area.MaxZ),
                    Fill   = fill,
                    Border = border,
                });
        }

        return result;
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
