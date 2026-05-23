using Avalonia;
using System;

namespace AnimationEditor.App;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // AfterSetup fires after UsePlatformDetect() loads Foundation/AppKit,
            // so NSProcessInfo is accessible and setProcessName: takes effect.
            .AfterSetup(_ => MacOSDockIcon.SetProcessName("Animation Editor"))
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
