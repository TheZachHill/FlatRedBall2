using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Regression tests for issue #239: modal confirmation dialogs must be
/// keyboard-accessible — ENTER confirms, ESC cancels, immediately on open
/// without a prior click. Covers the yes/no confirmation dialog and the shared
/// <see cref="MainWindow.WireDialogKeyboard"/> helper used by the dialogs that
/// contain input controls.
/// </summary>
public class ConfirmDialogKeyboardTests
{
    private static void RaiseKey(InputElement target, Key key) =>
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = target,
            Key = key,
        });

    // Simulates real keyboard input through the focus pipeline — unlike RaiseEvent,
    // this only reaches a handler if something inside the window is actually focused.
    private static void PressKey(Window dialog, Key key) =>
        dialog.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);

    [AvaloniaFact]
    public void BuildConfirmDialog_EnterKey_ResolvesTrue()
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = MainWindow.BuildConfirmDialog("Delete this frame?", "Delete?", tcs);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        PressKey(dialog, Key.Enter);
        Dispatcher.UIThread.RunJobs();

        Assert.True(tcs.Task.IsCompletedSuccessfully, "ENTER should dismiss the dialog");
        Assert.True(tcs.Task.Result, "ENTER should confirm (Yes)");
    }

    [AvaloniaFact]
    public void BuildConfirmDialog_EscapeKey_ResolvesFalse()
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = MainWindow.BuildConfirmDialog("Delete this frame?", "Delete?", tcs);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        PressKey(dialog, Key.Escape);
        Dispatcher.UIThread.RunJobs();

        Assert.True(tcs.Task.IsCompletedSuccessfully, "ESC should dismiss the dialog");
        Assert.False(tcs.Task.Result, "ESC should cancel (No)");
    }

    [AvaloniaFact]
    public void BuildConfirmDialog_ClosedWithoutChoice_ResolvesFalse()
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = MainWindow.BuildConfirmDialog("Delete this frame?", "Delete?", tcs);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        dialog.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.True(tcs.Task.IsCompletedSuccessfully);
        Assert.False(tcs.Task.Result, "Closing the dialog without choosing must not delete");
    }

    // ── WireDialogKeyboard ────────────────────────────────────────────────────

    /// <summary>
    /// A freshly-opened window has no focused element, so keyboard input is not
    /// routed anywhere. WireDialogKeyboard must move focus into the dialog on open
    /// so ENTER/ESC work without the user clicking a control first.
    /// </summary>
    [AvaloniaFact]
    public void WireDialogKeyboard_OnOpen_MovesFocusIntoDialog()
    {
        var dialog = new Window
        {
            Content = new StackPanel
            {
                Children = { new Button { Content = "OK" }, new Button { Content = "Cancel" } }
            }
        };
        MainWindow.WireDialogKeyboard(dialog, onConfirm: () => { }, onCancel: () => { });
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        var focused = dialog.FocusManager?.GetFocusedElement();
        Assert.NotNull(focused);
        Assert.Contains((Visual)focused!, dialog.GetVisualDescendants());
        dialog.Close();
    }

    /// <summary>
    /// Drives real keyboard input through the focus pipeline on an untouched
    /// dialog containing a NumericUpDown — ESC must cancel. (The headless input
    /// backend routes unfocused keys to the window, so the no-focus gap itself is
    /// guarded by <see cref="WireDialogKeyboard_OnOpen_MovesFocusIntoDialog"/>;
    /// this covers the input path end-to-end.)
    /// </summary>
    [AvaloniaFact]
    public void WireDialogKeyboard_FreshDialog_EscapeCancelsWithoutClickingFirst()
    {
        bool cancelled = false;
        var dialog = new Window
        {
            Content = new StackPanel
            {
                Children = { new NumericUpDown { Value = 1 }, new Button { Content = "OK" } }
            }
        };
        MainWindow.WireDialogKeyboard(dialog, onConfirm: () => { }, onCancel: () => cancelled = true);
        dialog.Show();
        Dispatcher.UIThread.RunJobs();

        PressKey(dialog, Key.Escape);
        Dispatcher.UIThread.RunJobs();

        Assert.True(cancelled, "ESC must cancel a freshly-opened dialog without a prior click");
        dialog.Close();
    }

    // The Adjust Offsets / Resize Texture / Adjust Frame Time dialogs contain
    // input controls (NumericUpDown) whose focused inner control can mark the
    // key event handled before it bubbles to the window — so Button.IsCancel
    // never fires. WireDialogKeyboard attaches at the window with
    // handledEventsToo:true to survive that.

    [AvaloniaFact]
    public void WireDialogKeyboard_EscapeHandledByChild_StillCancels()
    {
        bool confirmed = false, cancelled = false;
        var inner = new Button();                       // a focusable child
        var dialog = new Window { Content = inner };
        MainWindow.WireDialogKeyboard(dialog,
            onConfirm: () => confirmed = true,
            onCancel:  () => cancelled = true);
        // Simulate an input control that swallows Escape before it bubbles up.
        inner.AddHandler(InputElement.KeyDownEvent,
            (object? _, KeyEventArgs e) => { if (e.Key == Key.Escape) e.Handled = true; });

        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        RaiseKey(inner, Key.Escape);
        Dispatcher.UIThread.RunJobs();

        Assert.True(cancelled, "ESC must cancel even when a focused child marks the event handled");
        Assert.False(confirmed);
        dialog.Close();
    }

    [AvaloniaFact]
    public void WireDialogKeyboard_EnterHandledByChild_StillConfirms()
    {
        bool confirmed = false, cancelled = false;
        var inner = new Button();
        var dialog = new Window { Content = inner };
        MainWindow.WireDialogKeyboard(dialog,
            onConfirm: () => confirmed = true,
            onCancel:  () => cancelled = true);
        inner.AddHandler(InputElement.KeyDownEvent,
            (object? _, KeyEventArgs e) => { if (e.Key == Key.Enter) e.Handled = true; });

        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        RaiseKey(inner, Key.Enter);
        Dispatcher.UIThread.RunJobs();

        Assert.True(confirmed, "ENTER must confirm even when a focused child marks the event handled");
        Assert.False(cancelled);
        dialog.Close();
    }
}
