using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AnimationEditor.Core.CommandsAndState.Commands;
using FlatRedBall2.Animation.Content;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Verifies that HistoryUndoButton and HistoryRedoButton enable/disable
/// to reflect the current undo/redo availability.
/// </summary>
public class HistoryPanelButtonStateTests
{
    private static (MainWindow Window, TestServices Ctx) CreateWindow()
    {
        var ctx = TestHelpers.BuildServices();
        ctx.ProjectManager.AnimationChainListSave = new AnimationChainListSave();
        ctx.ProjectManager.FileName = null;
        var window = ctx.CreateMainWindow();
        window.Show();
        return (window, ctx);
    }

    // ── Initial state ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void HistoryUndoButton_DisabledInitially()
    {
        var (window, _) = CreateWindow();
        try
        {
            var btn = window.FindControl<Button>("HistoryUndoButton")!;
            Assert.False(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HistoryRedoButton_DisabledInitially()
    {
        var (window, _) = CreateWindow();
        try
        {
            var btn = window.FindControl<Button>("HistoryRedoButton")!;
            Assert.False(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    // ── After recording a command ─────────────────────────────────────────────

    [AvaloniaFact]
    public void HistoryUndoButton_EnabledAfterCommandRecorded()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryUndoButton")!;
            Assert.True(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HistoryRedoButton_StillDisabledAfterCommandRecorded()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryRedoButton")!;
            Assert.False(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    // ── After undoing ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void HistoryRedoButton_EnabledAfterUndo()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            ctx.UndoManager.Undo();
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryRedoButton")!;
            Assert.True(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HistoryUndoButton_DisabledAfterAllCommandsUndone()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            ctx.UndoManager.Undo();
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryUndoButton")!;
            Assert.False(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    // ── After redoing ─────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void HistoryRedoButton_DisabledAfterRedo()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            ctx.UndoManager.Undo();
            ctx.UndoManager.Redo();
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryRedoButton")!;
            Assert.False(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HistoryUndoButton_ReEnabledAfterRedo()
    {
        var (window, ctx) = CreateWindow();
        try
        {
            ctx.UndoManager.Record(new StubCmd());
            ctx.UndoManager.Undo();
            ctx.UndoManager.Redo();
            Dispatcher.UIThread.RunJobs();

            var btn = window.FindControl<Button>("HistoryUndoButton")!;
            Assert.True(btn.IsEnabled);
        }
        finally { window.Close(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class StubCmd : IUndoableCommand
    {
        public string Description => "Stub";
        public bool Do() => true;
        public void Undo() { }
    }
}
