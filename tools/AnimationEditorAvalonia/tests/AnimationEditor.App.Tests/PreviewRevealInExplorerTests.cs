using AnimationEditor.App.Controls;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FlatRedBall2.Animation.Content;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Issue #573 — right-clicking the Preview pane (away from any guide) shows a
/// "View &lt;filename&gt; in Explorer" context menu item for the currently previewed texture.
/// </summary>
public class PreviewRevealInExplorerTests
{
    private static void OpenContextMenu(PreviewControl ctrl) =>
        typeof(PreviewControl)
            .GetMethod("OnContextMenuOpening", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(ctrl, new object?[] { null, new CancelEventArgs() });

    // ── ResolveSelectedTexturePath ────────────────────────────────────────────

    [AvaloniaFact]
    public void ResolveSelectedTexturePath_NoSelection_ReturnsNull()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();

        Assert.Null(ctrl.ResolveSelectedTexturePath());
    }

    [AvaloniaFact]
    public void ResolveSelectedTexturePath_FrameHasNoTexture_ReturnsNull()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();
        var chain = new AnimationChainSave { Name = "Walk" };
        var frame = new AnimationFrameSave { TextureName = "" };
        chain.Frames.Add(frame);
        ctx.SelectedState.SelectedChain = chain;
        ctx.SelectedState.SelectedFrame = frame;

        Assert.Null(ctrl.ResolveSelectedTexturePath());
    }

    [AvaloniaFact]
    public void ResolveSelectedTexturePath_ResolvesRelativeToAchxDirectory()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();
        var chain = new AnimationChainSave { Name = "Walk" };
        var frame = new AnimationFrameSave { TextureName = "textures/hero.png" };
        chain.Frames.Add(frame);
        ctx.SelectedState.SelectedChain = chain;
        ctx.SelectedState.SelectedFrame = frame;
        ctx.ProjectManager.FileName = @"C:\proj\anim.achx";

        Assert.Equal("C:/proj/textures/hero.png", ctrl.ResolveSelectedTexturePath());
    }

    // ── OnContextMenuOpening ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void OnContextMenuOpening_NoSelection_CancelsAndLeavesMenuEmpty()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();

        OpenContextMenu(ctrl);

        Assert.Empty(ctrl.ContextMenu!.Items);
    }

    [AvaloniaFact]
    public void OnContextMenuOpening_TextureSelected_AddsRevealItemWithFilename()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();
        var chain = new AnimationChainSave { Name = "Walk" };
        var frame = new AnimationFrameSave { TextureName = "textures/hero.png" };
        chain.Frames.Add(frame);
        ctx.SelectedState.SelectedChain = chain;
        ctx.SelectedState.SelectedFrame = frame;
        ctx.ProjectManager.FileName = @"C:\proj\anim.achx";

        OpenContextMenu(ctrl);

        var item = ctrl.ContextMenu!.Items.OfType<MenuItem>().Single();
        Assert.Equal("View hero.png in Explorer", item.Header);
    }

    [AvaloniaFact]
    public void OnContextMenuOpening_ClickingItem_ReportsMissingFileViaShowError()
    {
        var ctx = TestHelpers.BuildServices();
        string? reportedError = null;
        var ctrl = ctx.CreatePreviewControl(msg => reportedError = msg);
        var chain = new AnimationChainSave { Name = "Walk" };
        var frame = new AnimationFrameSave { TextureName = "textures/hero.png" };
        chain.Frames.Add(frame);
        ctx.SelectedState.SelectedChain = chain;
        ctx.SelectedState.SelectedFrame = frame;
        ctx.ProjectManager.FileName = @"C:\proj\anim.achx";

        OpenContextMenu(ctrl);
        var item = ctrl.ContextMenu!.Items.OfType<MenuItem>().Single();
        item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));

        Assert.NotNull(reportedError);
    }

    // ── Right-click suppression (guide vs. menu) ───────────────────────────────

    [AvaloniaFact]
    public void SimulateRightClick_HitsGuide_ReturnsTrue()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();
        ctrl.Measure(new Avalonia.Size(64, 64));
        ctrl.Arrange(new Avalonia.Rect(0, 0, 64, 64));
        ctrl.AddHGuide(0f); // world Y=0 -> screen Y=42

        Assert.True(ctrl.SimulateRightClick(30f, 42f));
    }

    [AvaloniaFact]
    public void SimulateRightClick_MissesGuide_ReturnsFalse()
    {
        var ctx = TestHelpers.BuildServices();
        var ctrl = ctx.CreatePreviewControl();
        ctrl.Measure(new Avalonia.Size(64, 64));
        ctrl.Arrange(new Avalonia.Rect(0, 0, 64, 64));
        ctrl.AddHGuide(0f); // world Y=0 -> screen Y=42

        Assert.False(ctrl.SimulateRightClick(30f, 20f));
    }
}
