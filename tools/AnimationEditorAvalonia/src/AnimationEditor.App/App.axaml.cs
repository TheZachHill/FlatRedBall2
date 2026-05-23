using AnimationEditor.App.Services;
using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.CommandsAndState.Commands;
using AnimationEditor.Core.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AnimationEditor.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = BuildServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = services.GetRequiredService<MainWindow>();
            // Set the icon in code so Avalonia's macOS backend propagates it to
            // NSApplication.SharedApplication.ApplicationIconImage for the Dock.
            // The XAML attribute alone is not always sufficient on macOS when
            // WindowDecorations="None" bypasses the native NSWindow title-bar path.
            mainWindow.Icon = LoadAppIcon();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static WindowIcon LoadAppIcon()
    {
        using var stream = AssetLoader.Open(
            new Uri("avares://AnimationEditor.App/Assets/icons/achx-icon-256.png"));
        return new WindowIcon(new Bitmap(stream));
    }

    private static ServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();

        sc.AddSingleton<ProjectManager>();
        sc.AddSingleton<IProjectManager>(sp => sp.GetRequiredService<ProjectManager>());

        sc.AddSingleton<ApplicationEvents>();
        sc.AddSingleton<IApplicationEvents>(sp => sp.GetRequiredService<ApplicationEvents>());

        sc.AddSingleton<SelectedState>(sp =>
            new SelectedState(sp.GetRequiredService<IProjectManager>()));
        sc.AddSingleton<ISelectedState>(sp => sp.GetRequiredService<SelectedState>());

        sc.AddSingleton<AppState>(sp =>
            new AppState(sp.GetRequiredService<IApplicationEvents>(),
                         sp.GetRequiredService<ISelectedState>()));
        sc.AddSingleton<IAppState>(sp => sp.GetRequiredService<AppState>());

        sc.AddSingleton<IoManager>(sp =>
            new IoManager(sp.GetRequiredService<IAppState>()));
        sc.AddSingleton<IIoManager>(sp => sp.GetRequiredService<IoManager>());

        sc.AddSingleton<ObjectFinder>(sp =>
            new ObjectFinder(sp.GetRequiredService<IProjectManager>()));
        sc.AddSingleton<IObjectFinder>(sp => sp.GetRequiredService<ObjectFinder>());

        sc.AddSingleton<UndoManager>();
        sc.AddSingleton<IUndoManager>(sp => sp.GetRequiredService<UndoManager>());

        sc.AddSingleton<AppCommands>(sp =>
            new AppCommands(
                sp.GetRequiredService<IProjectManager>(),
                sp.GetRequiredService<ISelectedState>(),
                sp.GetRequiredService<IApplicationEvents>(),
                sp.GetRequiredService<IIoManager>(),
                sp.GetRequiredService<IObjectFinder>(),
                sp.GetRequiredService<IUndoManager>()));
        sc.AddSingleton<IAppCommands>(sp => sp.GetRequiredService<AppCommands>());

        sc.AddSingleton<ThumbnailService>(sp =>
            new ThumbnailService(sp.GetRequiredService<IProjectManager>()));

        sc.AddTransient<MainWindow>(sp => new MainWindow(
            sp.GetRequiredService<IProjectManager>(),
            sp.GetRequiredService<ISelectedState>(),
            sp.GetRequiredService<IAppCommands>(),
            sp.GetRequiredService<IAppState>(),
            sp.GetRequiredService<IApplicationEvents>(),
            sp.GetRequiredService<IIoManager>(),
            sp.GetRequiredService<IObjectFinder>(),
            sp.GetRequiredService<IUndoManager>(),
            sp.GetRequiredService<ThumbnailService>()));

        return sc.BuildServiceProvider();
    }
}
