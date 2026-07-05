using AnimationEditor.App.Controls;
using AnimationEditor.Core.IO;
using Avalonia.Headless.XUnit;
using FlatRedBall2.Animation.Content;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Regression tests for issue #582: when multiple frames are multi-selected in the tree
/// (SelectedNodes holds several AnimationFrameSave), the wireframe must show all of their
/// regions — not just the primary SelectedFrame. Companion to WireframeMultiChainTests
/// (whole-chain multi-select) and MultiSelectPropertyPanelTests (the #571 property-edit fix
/// that introduced ISelectedState.SelectedFrames).
///
/// Camera fixed at pan=(0,0) zoom=1 so texture pixels == screen pixels. Texture size: 64 x 64.
/// </summary>
public class WireframeMultiSelectFramePreviewTests
{
    private static TestServices ResetSingletons()
    {
        var ctx = TestHelpers.BuildServices();
        ctx.ProjectManager.AnimationChainListSave = new AnimationChainListSave();
        ctx.ProjectManager.FileName               = null;
        ctx.SelectedState.SelectedChain           = null;
        ctx.SelectedState.SelectedFrame           = null;
        ctx.SelectedState.SelectedNodes           = new List<object>();
        ctx.AppCommands.DoOnUiThread              = a => a();
        ctx.AppCommands.FileDialogService         = NullFileDialogService.Instance;
        ctx.AppState.OffsetMultiplier             = 1f;
        return ctx;
    }

    private static string WriteSolidPng(string dir, SKColor color, int size = 64,
                                         string name = "sprite.png")
    {
        var path = Path.Combine(dir, name);
        using var bm = new SKBitmap(size, size);
        bm.Erase(color);
        using var data = bm.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    /// <summary>
    /// One chain with three frames on the same 64x64 texture, tiled left-to-right.
    /// f0/f1 are multi-selected in the tree (SelectedFrame = f0 is the anchor);
    /// f2 is not part of the selection.
    /// </summary>
    private static (WireframeControl ctrl, AnimationFrameSave f0, AnimationFrameSave f1,
                    AnimationFrameSave f2, string dir)
        BuildMultiFrameSelectCtrl(TestServices ctx)
    {
        var dir = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var png = WriteSolidPng(dir, SKColors.DarkGray, name: "sprite.png");

        var f0 = new AnimationFrameSave
        {
            TextureName      = "sprite.png",
            LeftCoordinate   = 0f,      TopCoordinate    = 0f,
            RightCoordinate  = 1f / 3f, BottomCoordinate = 1f,
            FrameLength      = 0.1f, ShapesSave = new ShapesSave()
        };
        var f1 = new AnimationFrameSave
        {
            TextureName      = "sprite.png",
            LeftCoordinate   = 1f / 3f, TopCoordinate    = 0f,
            RightCoordinate  = 2f / 3f, BottomCoordinate = 1f,
            FrameLength      = 0.1f, ShapesSave = new ShapesSave()
        };
        var f2 = new AnimationFrameSave
        {
            TextureName      = "sprite.png",
            LeftCoordinate   = 2f / 3f, TopCoordinate    = 0f,
            RightCoordinate  = 1f,      BottomCoordinate = 1f,
            FrameLength      = 0.1f, ShapesSave = new ShapesSave()
        };

        var chain = new AnimationChainSave { Name = "Walk" };
        chain.Frames.AddRange(new[] { f0, f1, f2 });

        ctx.ProjectManager.AnimationChainListSave!.AnimationChains.Add(chain);
        ctx.ProjectManager.FileName = Path.Combine(dir, "test.achx");

        // Tree multi-select: f0 is the anchor (SelectedFrame), bag holds f0 + f1.
        ctx.SelectedState.SelectedFrame = f0;
        ctx.SelectedState.SelectedNodes = new List<object> { f0, f1 };

        var ctrl = ctx.CreateWireframeControl();
        ctrl.LoadTexture(png);
        ctrl.SetCamera(0f, 0f, 1f);
        ctrl.RefreshFrames();

        return (ctrl, f0, f1, f2, dir);
    }

    [AvaloniaFact]
    public void MultiSelectFrames_BothSelectedFramesRenderInWireframe()
    {
        var ctx = ResetSingletons();
        var (ctrl, f0, f1, f2, dir) = BuildMultiFrameSelectCtrl(ctx);
        try
        {
            Assert.Equal(2, ctrl.FrameRectCount);

            var rects = ctrl.GetFrameRects();
            // Both selected frames must be present and highlighted as selected.
            Assert.All(rects, r => Assert.True(r.IsSelected));
        }
        finally { Directory.Delete(dir, true); }
    }

    [AvaloniaFact]
    public void MultiSelectFrames_UnselectedThirdFrameDoesNotRender()
    {
        var ctx = ResetSingletons();
        var (ctrl, f0, f1, f2, dir) = BuildMultiFrameSelectCtrl(ctx);
        try
        {
            // Only f0/f1's bounds should be present; f2's left edge (2/3 * 64) is not among them.
            var rects = ctrl.GetFrameRects();
            Assert.DoesNotContain(rects, r => System.Math.Abs(r.Bounds.Left - (2f / 3f * 64f)) < 0.01f);
        }
        finally { Directory.Delete(dir, true); }
    }

    [AvaloniaFact]
    public void SingleFrameSelected_OnlyThatFrameRenders_RegressionForSinglSelect()
    {
        var ctx = ResetSingletons();
        var (ctrl, f0, f1, f2, dir) = BuildMultiFrameSelectCtrl(ctx);
        try
        {
            // Collapse back to a single-frame selection (as if the user clicked just one frame).
            ctx.SelectedState.SelectedNodes = new List<object>();
            ctx.SelectedState.SelectedFrame = f1;
            ctrl.RefreshFrames();

            Assert.Equal(1, ctrl.FrameRectCount);
            Assert.True(ctrl.GetFrameRects().Single().IsSelected);
        }
        finally { Directory.Delete(dir, true); }
    }
}
