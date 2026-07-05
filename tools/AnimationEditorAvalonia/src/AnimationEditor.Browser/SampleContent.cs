using System;

namespace AnimationEditor.Browser;

/// <summary>
/// Bundled sample .achx text + texture bytes, fetched over HTTP by <see cref="Program.Main"/>
/// before Avalonia starts. Static because <see cref="App.OnFrameworkInitializationCompleted"/>
/// is synchronous and needs the content already in hand by the time it runs.
/// </summary>
internal static class SampleContent
{
    public static string AchxText = string.Empty;
    public static byte[] PngBytes = Array.Empty<byte>();
}
