using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Verifies that the app icon asset is wired correctly so macOS can display it in the Dock.
/// </summary>
public class AppIconTests
{
    private const string IconUri = "avares://AnimationEditor.App/Assets/icons/achx-icon-256.png";

    /// <summary>
    /// The icon asset must exist and be decodable as a bitmap. This is the resource that
    /// Avalonia propagates to NSApplication.SharedApplication.ApplicationIconImage on macOS.
    /// </summary>
    [AvaloniaFact]
    public void IconAsset_IsLoadable()
    {
        using var stream = AssetLoader.Open(new Uri(IconUri));
        Assert.NotNull(stream);
        var bitmap = new Bitmap(stream);
        Assert.True(bitmap.PixelSize.Width > 0);
        Assert.True(bitmap.PixelSize.Height > 0);
    }

    /// <summary>
    /// MainWindow must have its Icon property set (not null) so Avalonia propagates it to the
    /// macOS Dock. Belt-and-suspenders over the XAML attribute: ensures the programmatic setter
    /// in App.axaml.cs actually fires.
    /// </summary>
    [AvaloniaFact]
    public void MainWindow_HasIconSet()
    {
        var ctx = TestHelpers.BuildServices();
        var window = ctx.CreateMainWindow();
        window.Show();
        try
        {
            Assert.NotNull(window.Icon);
        }
        finally
        {
            window.Close();
        }
    }
}
