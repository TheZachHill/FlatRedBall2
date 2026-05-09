using AnimationEditor.App.Controls;
using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.Data;
using AnimationEditor.Core.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Tests that the bottom-preview zoom combo box stays in sync with the
/// PreviewControl's actual zoom when the user mouse-wheels over the preview.
///
/// Issue #109 — before this fix, wheel-zooming the bottom preview changed
/// the camera but left the PreviewZoomCombo selection stale.
/// </summary>
public class PreviewZoomComboSyncTests
{
    private static void ResetSingletons()
    {
        ProjectManager.Self.AnimationChainListSave = new AnimationChainListSave();
        ProjectManager.Self.FileName               = null;
        SelectedState.Self.SelectedChain           = null;
        SelectedState.Self.SelectedFrame           = null;
        SelectedState.Self.SelectedNodes           = new System.Collections.Generic.List<object>();
        AppCommands.Self.DoOnUiThread              = a => a();
        AppCommands.Self.ConfirmAsync              = (_, _) => Task.FromResult(true);
        AppCommands.Self.FileDialogService         = NullFileDialogService.Instance;
        AppState.Self.UnitType                     = UnitType.Pixel;
    }

    private static T FindCtrl<T>(MainWindow w, string name) where T : Control
        => w.FindControl<T>(name)
           ?? throw new InvalidOperationException($"Control '{name}' not found");

    [AvaloniaFact]
    public void PreviewControl_FiresZoomChanged_OnWheelZoom()
    {
        var preview = new PreviewControl();
        // Force a non-zero bounds so ApplyWheelZoom math is exercised.
        preview.Measure(new Size(400, 300));
        preview.Arrange(new Rect(0, 0, 400, 300));

        float? lastPct = null;
        preview.ZoomChanged += pct => lastPct = pct;

        preview.SimulateWheelZoom(100, 100, zoomIn: true);

        Assert.NotNull(lastPct);
        // 1.0 * 1.25 = 1.25 → 125 %
        Assert.Equal(125f, lastPct!.Value, precision: 2);
    }

    [AvaloniaFact]
    public void PreviewZoomCombo_SyncsToNearestPreset_AfterWheelZoomOnPreview()
    {
        ResetSingletons();

        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var preview = FindCtrl<PreviewControl>(window, "PreviewCtrl");
        var combo   = FindCtrl<ComboBox>(window, "PreviewZoomCombo");

        // Default selected index is 3 (100%). Two zoom-in notches → 1.5625× → 156 % ≈ nearest preset 200 %.
        preview.SimulateWheelZoom(100, 100, zoomIn: true);
        preview.SimulateWheelZoom(100, 100, zoomIn: true);
        Dispatcher.UIThread.RunJobs();

        // Combo presets are { 10, 25, 50, 100, 200, 400 }; nearest to 156 is 200 (index 4).
        Assert.Equal(4, combo.SelectedIndex);

        window.Close();
    }
}
