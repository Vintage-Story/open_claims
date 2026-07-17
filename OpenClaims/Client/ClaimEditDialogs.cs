using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace OpenClaims.Client;

public class ClaimRenameDialog : GuiDialog
{
    private readonly int claimIndex;
    private readonly Action<int, string> onConfirm;

    public override string ToggleKeyCombinationCode => null!;
    public override double DrawOrder => 2.0;

    public ClaimRenameDialog(ICoreClientAPI capi, int claimIndex, string currentName, Action<int, string> onConfirm, double x = -1, double y = -1)
        : base(capi)
    {
        this.claimIndex = claimIndex;
        this.onConfirm  = onConfirm;
        Compose(currentName, x, y);
    }

    private void Compose(string currentName, double x, double y)
    {
        var dlgBounds = (x >= 0 && y >= 0)
            ? ElementStdBounds.AutosizedMainDialog.WithFixedPosition(x, y).WithAlignment(EnumDialogArea.None)
            : ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed);
        var bgBounds  = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var inputBounds     = ElementBounds.Fixed(0,  30, 250, 30);
        var btnOkBounds     = ElementBounds.Fixed(0,  70, 120, 25);
        var btnCancelBounds = ElementBounds.Fixed(130, 70, 120, 25);

        SingleComposer = capi.Gui.CreateCompo("openclaims-rename", dlgBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("openclaims:rename_title"), () => TryClose())
            .BeginChildElements(bgBounds)
            .AddTextInput(inputBounds, null, CairoFont.WhiteSmallText(), "nameInput")
            .AddSmallButton(Lang.Get("openclaims:btn_confirm"), OnOk, btnOkBounds)
            .AddSmallButton(Lang.Get("openclaims:btn_cancel"), () => { TryClose(); return true; }, btnCancelBounds)
            .EndChildElements()
            .Compose();

        SingleComposer.GetTextInput("nameInput").SetValue(currentName);
    }

    private bool OnOk()
    {
        string name = SingleComposer.GetTextInput("nameInput").GetText().Trim();
        if (name.Length > 0)
            onConfirm(claimIndex, name);
        TryClose();
        return true;
    }
}

public class ClaimAllowDialog : GuiDialog
{
    private readonly int claimIndex;
    private readonly Action<int, string> onConfirm;

    public override string ToggleKeyCombinationCode => null!;
    public override double DrawOrder => 2.0;

    public ClaimAllowDialog(ICoreClientAPI capi, int claimIndex, Action<int, string> onConfirm, double x = -1, double y = -1)
        : base(capi)
    {
        this.claimIndex = claimIndex;
        this.onConfirm  = onConfirm;
        Compose(x, y);
    }

    private void Compose(double x, double y)
    {
        var dlgBounds = (x >= 0 && y >= 0)
            ? ElementStdBounds.AutosizedMainDialog.WithFixedPosition(x, y).WithAlignment(EnumDialogArea.None)
            : ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var inputBounds     = ElementBounds.Fixed(0,   30, 250, 30);
        var btnOkBounds     = ElementBounds.Fixed(0,   70, 120, 25);
        var btnCancelBounds = ElementBounds.Fixed(130, 70, 120, 25);

        SingleComposer = capi.Gui.CreateCompo("openclaims-allow", dlgBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("openclaims:allow_title"), () => TryClose())
            .BeginChildElements(bgBounds)
            .AddTextInput(inputBounds, null, CairoFont.WhiteSmallText(), "playerInput")
            .AddSmallButton(Lang.Get("openclaims:btn_confirm"), OnOk, btnOkBounds)
            .AddSmallButton(Lang.Get("openclaims:btn_cancel"), () => { TryClose(); return true; }, btnCancelBounds)
            .EndChildElements()
            .Compose();
    }

    private bool OnOk()
    {
        string name = SingleComposer.GetTextInput("playerInput").GetText().Trim();
        if (name.Length > 0)
            onConfirm(claimIndex, name);
        TryClose();
        return true;
    }
}

public class ClaimUnallowDialog : GuiDialog
{
    private readonly int claimIndex;
    private readonly Action<int, string> onRemove;
    private readonly List<(string uid, string name)> players;

    public override string ToggleKeyCombinationCode => null!;
    public override double DrawOrder => 2.0;

    public ClaimUnallowDialog(ICoreClientAPI capi, int claimIndex,
        List<(string uid, string name)> players, Action<int, string> onRemove,
        double x = -1, double y = -1)
        : base(capi)
    {
        this.claimIndex = claimIndex;
        this.players    = players;
        this.onRemove   = onRemove;
        Compose(x, y);
    }

    private void Compose(double x, double y)
    {
        const double W    = 260.0;
        const double BtnW = 75.0;
        const double RowH = 25.0;
        const double RowGap = 4.0;

        var dlgBounds = (x >= 0 && y >= 0)
            ? ElementStdBounds.AutosizedMainDialog.WithFixedPosition(x, y).WithAlignment(EnumDialogArea.None)
            : ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var composer = capi.Gui.CreateCompo("openclaims-unallow", dlgBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("openclaims:unallow_title"), () => TryClose())
            .BeginChildElements(bgBounds);

        if (players.Count == 0)
        {
            var emptyBounds  = ElementBounds.Fixed(0, 30, W, RowH);
            var closeBounds  = ElementBounds.Fixed(0, 30 + RowH + 8, W, RowH);
            composer
                .AddStaticText(Lang.Get("openclaims:unallow_empty"), CairoFont.WhiteSmallText(), emptyBounds)
                .AddSmallButton(Lang.Get("openclaims:btn_cancel"), () => { TryClose(); return true; }, closeBounds);
        }
        else
        {
            for (int i = 0; i < players.Count; i++)
            {
                string uid  = players[i].uid;
                string name = players[i].name;
                double rowY = 30 + i * (RowH + RowGap);
                var nameBounds = ElementBounds.Fixed(0,         rowY, W - BtnW - 10, RowH);
                var btnBounds  = ElementBounds.Fixed(W - BtnW,  rowY, BtnW,          RowH);
                string capturedUid = uid;
                composer
                    .AddStaticText(name, CairoFont.WhiteSmallText(), nameBounds)
                    .AddSmallButton(Lang.Get("openclaims:link_unallow"), () => OnRemove(capturedUid), btnBounds, EnumButtonStyle.Normal, $"btn_unallow_{i}");
            }
        }

        SingleComposer = composer.EndChildElements().Compose();
    }

    private bool OnRemove(string uid)
    {
        onRemove(claimIndex, uid);
        TryClose();
        return true;
    }
}

public class ClaimConfirmDialog : GuiDialog
{
    private readonly Action onConfirm;

    public override string ToggleKeyCombinationCode => null!;
    public override double DrawOrder => 2.0;

    public ClaimConfirmDialog(ICoreClientAPI capi, string message, Action onConfirm, double x = -1, double y = -1)
        : base(capi)
    {
        this.onConfirm = onConfirm;
        Compose(message, x, y);
    }

    private void Compose(string message, double x, double y)
    {
        var dlgBounds = (x >= 0 && y >= 0)
            ? ElementStdBounds.AutosizedMainDialog.WithFixedPosition(x, y).WithAlignment(EnumDialogArea.None)
            : ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed);
        var bgBounds  = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var textBounds      = ElementBounds.Fixed(0,  30, 250, 25);
        var btnOkBounds     = ElementBounds.Fixed(0,  65, 120, 25);
        var btnCancelBounds = ElementBounds.Fixed(130, 65, 120, 25);

        SingleComposer = capi.Gui.CreateCompo("openclaims-confirm", dlgBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(Lang.Get("openclaims:confirm_title"), () => TryClose())
            .BeginChildElements(bgBounds)
            .AddStaticText(message, CairoFont.WhiteSmallText(), textBounds)
            .AddSmallButton(Lang.Get("openclaims:btn_confirm"), OnOk, btnOkBounds)
            .AddSmallButton(Lang.Get("openclaims:btn_cancel"), () => { TryClose(); return true; }, btnCancelBounds)
            .EndChildElements()
            .Compose();
    }

    private bool OnOk()
    {
        onConfirm();
        TryClose();
        return true;
    }
}
