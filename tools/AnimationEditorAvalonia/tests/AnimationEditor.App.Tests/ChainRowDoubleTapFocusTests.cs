using System.Reflection;
using AnimationEditor.App.Controls;
using AnimationEditor.Core.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FlatRedBall2.Animation.Content;
using SkiaSharp;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Issue #716: double-tapping blank row space on a chain (not its label, not the Add-Frame
/// button) used to route through the same code path as label double-tap
/// (<see cref="MainWindow.HandleAnimTreeNodeDoubleTap"/> unconditionally called
/// <c>BeginInlineRename</c>), which put the row into rename mode while the tree's own native
/// double-click handling raced it. The desired behaviour is a "focus" action instead — fit the
/// chain's frames into the wireframe view — so rename is now reserved for the label double-tap
/// specifically, and blank-space double-tap fits the chain.
/// </summary>
public class ChainRowDoubleTapFocusTests
{
    private static (MainWindow Window, TestServices Ctx) CreateWindow()
    {
        var ctx = TestHelpers.BuildServices();
        ctx.ProjectManager.AnimationChainListSave = new AnimationChainListSave();
        ctx.ProjectManager.FileName = null;
        var window = ctx.CreateMainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, ctx);
    }

    private static TreeView GetTree(MainWindow w)
        => w.FindControl<TreeView>("AnimTree")
           ?? throw new InvalidOperationException("AnimTree control not found");

    private static void TriggerRefreshTreeView(MainWindow window)
    {
        typeof(MainWindow).GetMethod("RefreshTreeView", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(window, null);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>FitChainToView no-ops without a loaded texture, so tests that need it to actually
    /// move the camera load a throwaway solid-color PNG first.</summary>
    private static string WriteSolidPng(string dir, string name, int width, int height)
    {
        var path = Path.Combine(dir, name);
        using var bm = new SKBitmap(width, height);
        bm.Erase(SKColors.Blue);
        using var data = bm.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    /// <summary>Invokes the private OnAnimTreeDoubleTapped handler as if a real pointer double-tap
    /// landed on <paramref name="source"/> inside the chain's row (mirrors the existing
    /// DoubleTapOnPlusButtonDescendant_DoesNotTriggerChainRename regression test's approach).</summary>
    private static void RaiseBlankRowDoubleTap(MainWindow window, Control source)
    {
        var fakeArgs = (TappedEventArgs)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(TappedEventArgs));
        fakeArgs.Source = source;

        var handler = typeof(MainWindow).GetMethod(
            "OnAnimTreeDoubleTapped", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnAnimTreeDoubleTapped not found");
        handler.Invoke(window, [null, fakeArgs]);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void DoubleTapBlankRowSpace_OnChainWithFrames_FitsChainInsteadOfRenaming()
    {
        var (window, ctx) = CreateWindow();
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var chain = new AnimationChainSave { Name = "Walk" };
            chain.Frames.Add(new AnimationFrameSave
            {
                LeftCoordinate = 0.1f, TopCoordinate = 0.1f,
                RightCoordinate = 0.3f, BottomCoordinate = 0.3f,
            });
            ctx.ProjectManager.AnimationChainListSave!.AnimationChains.Add(chain);

            TriggerRefreshTreeView(window);

            var tree = GetTree(window);
            var chainNode = (TreeNodeVm)tree.ItemsSource!.Cast<object>().First();

            var tvi = tree.GetVisualDescendants().OfType<TreeViewItem>()
                .First(t => ReferenceEquals(t.DataContext, chainNode));
            // The row's icon StackPanel: a descendant of the TreeViewItem that is neither the
            // header TextBlock nor the Add-Frame Button — i.e. genuine "blank" row space.
            var iconPanel = tvi.GetVisualDescendants().OfType<StackPanel>().First();

            var wireframe = window.FindControl<WireframeControl>("WireframeCtrl")
                ?? throw new InvalidOperationException("WireframeCtrl not found");
            var texPath = WriteSolidPng(dir, "tex.png", 1000, 1000);
            wireframe.LoadTexture(texPath);
            Dispatcher.UIThread.RunJobs();
            wireframe.SetCamera(0f, 0f, 1f);

            RaiseBlankRowDoubleTap(window, iconPanel);

            Assert.False(chainNode.IsEditing,
                "Double-tap on blank row space must not start an inline rename.");
            var (panX, panY, zoom) = wireframe.CameraState;
            Assert.True(panX != 0f || panY != 0f || zoom != 1f,
                "Double-tap on blank row space must focus (fit) the chain's frames in the wireframe view.");
        }
        finally { window.Close(); Directory.Delete(dir, true); }
    }

    [AvaloniaFact]
    public void DoubleTapBlankRowSpace_OnEmptyChain_DoesNothing()
    {
        var (window, ctx) = CreateWindow();
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var chain = new AnimationChainSave { Name = "Empty" };
            ctx.ProjectManager.AnimationChainListSave!.AnimationChains.Add(chain);

            TriggerRefreshTreeView(window);

            var tree = GetTree(window);
            var chainNode = (TreeNodeVm)tree.ItemsSource!.Cast<object>().First();

            var tvi = tree.GetVisualDescendants().OfType<TreeViewItem>()
                .First(t => ReferenceEquals(t.DataContext, chainNode));
            var iconPanel = tvi.GetVisualDescendants().OfType<StackPanel>().First();

            var wireframe = window.FindControl<WireframeControl>("WireframeCtrl")
                ?? throw new InvalidOperationException("WireframeCtrl not found");
            var texPath = WriteSolidPng(dir, "tex.png", 1000, 1000);
            wireframe.LoadTexture(texPath);
            Dispatcher.UIThread.RunJobs();
            wireframe.SetCamera(12f, 34f, 2f);

            RaiseBlankRowDoubleTap(window, iconPanel);

            Assert.False(chainNode.IsEditing);
            var (panX, panY, zoom) = wireframe.CameraState;
            Assert.Equal(12f, panX, 3);
            Assert.Equal(34f, panY, 3);
            Assert.Equal(2f, zoom, 3);
        }
        finally { window.Close(); Directory.Delete(dir, true); }
    }

    [AvaloniaFact]
    public void DoubleTapHeaderLabel_OnChain_StillStartsRename()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            var chain = new AnimationChainSave { Name = "Walk" };
            ctx.ProjectManager.AnimationChainListSave!.AnimationChains.Add(chain);

            TriggerRefreshTreeView(window);

            var tree = GetTree(window);
            var chainNode = (TreeNodeVm)tree.ItemsSource!.Cast<object>().First();

            var tvi = tree.GetVisualDescendants().OfType<TreeViewItem>()
                .First(t => ReferenceEquals(t.DataContext, chainNode));
            var label = tvi.GetVisualDescendants().OfType<TextBlock>()
                .First(tb => ReferenceEquals(tb.DataContext, chainNode));

            var fakeArgs = (TappedEventArgs)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(TappedEventArgs));
            fakeArgs.Source = label;

            var handler = typeof(MainWindow).GetMethod(
                "OnHeaderTextDoubleTapped", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("OnHeaderTextDoubleTapped not found");
            // OnHeaderTextDoubleTapped reads `sender`, not e.Source, so pass the label there.
            handler.Invoke(window, [label, fakeArgs]);
            Dispatcher.UIThread.RunJobs();

            Assert.True(chainNode.IsEditing,
                "Double-tap on the chain's label must still start an inline rename.");
        }
        finally { window.Close(); }
    }
}
