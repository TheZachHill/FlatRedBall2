using System;
using AnimationEditor.App.Controls;
using AnimationEditor.App.Services;
using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.CommandsAndState.Commands;
using AnimationEditor.Core.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlatRedBall2.Animation.Content;
using SkiaSharp;
using FilePath = AnimationEditor.Core.Paths.FilePath;

namespace AnimationEditor.Browser;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = BuildSampleView();

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// #535 M2: wires the same service graph <c>MainWindow</c> builds on desktop (minus the
    /// desktop-only bits -- file association, native menu, single-instance IPC), loads the
    /// bundled sample fetched by <see cref="Program.Main"/>, and hands it to the real
    /// <see cref="PreviewControl"/> -- no file I/O anywhere in this path.
    /// </summary>
    private static PreviewControl BuildSampleView()
    {
        var projectManager    = new ProjectManager();
        var applicationEvents = new ApplicationEvents();
        var selectedState     = new SelectedState(projectManager);
        var appState          = new AppState(applicationEvents, selectedState);
        var ioManager         = new IoManager(appState);
        var objectFinder      = new ObjectFinder(projectManager);
        var undoManager       = new UndoManager();
        var pendingCutState   = new PendingCutState();
        var appCommands       = new AppCommands(
            projectManager, selectedState, applicationEvents, ioManager, objectFinder, undoManager);
        var thumbnailService  = new ThumbnailService(projectManager);

        var acls = AnimationChainListSave.FromString(SampleContent.AchxText);
        // "sample/player.achx" doesn't exist on disk (no filesystem in the browser); preParsed
        // means LoadAnimationChain never tries to read it, only uses it as a logical identity.
        projectManager.LoadAnimationChain(new FilePath("sample/player.achx"), acls);

        var bitmap = SKBitmap.Decode(SampleContent.PngBytes);
        thumbnailService.SeedTexture("player.png", bitmap);

        var preview = new PreviewControl();
        preview.InitializeServices(
            selectedState, appState, appCommands, applicationEvents,
            projectManager, undoManager, thumbnailService, pendingCutState);

        // Selecting a chain with no frame pinned auto-plays it (PreviewControl.OnSelectionChanged).
        selectedState.SelectedChain = acls.AnimationChains[0];

        return preview;
    }
}
