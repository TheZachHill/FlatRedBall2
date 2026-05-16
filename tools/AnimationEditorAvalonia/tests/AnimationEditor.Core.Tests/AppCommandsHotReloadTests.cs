using AnimationEditor.Core.HotReload;
using FlatRedBall2.Animation.Content;
using System.Collections.Generic;
using Xunit;

namespace AnimationEditor.Core.Tests;

/// <summary>
/// Verifies <see cref="AppCommands.SyncHotReloadWatcher"/> starts (or restarts) the
/// hot-reload watcher correctly for both unsaved and saved projects.
/// </summary>
public class AppCommandsHotReloadTests
{
    // ── SyncHotReloadWatcher – unsaved project ────────────────────────────────

    [Fact]
    public void SyncHotReloadWatcher_UnsavedProject_PassesEmptyAchxPath()
    {
        var ctx = TestHelpers.SetupFreshAcls();
        var spy = new SpyHotReloadWatcher();
        ctx.AppCommands.HotReloadWatcher = spy;
        ctx.ProjectManager.FileName = null;

        ctx.AppCommands.SyncHotReloadWatcher();

        Assert.Equal(string.Empty, spy.LastStartAchxPath);
    }

    [Fact]
    public void SyncHotReloadWatcher_UnsavedProjectWithAbsolutePng_StartsWatchingThatPng()
    {
        var ctx = TestHelpers.SetupFreshAcls();
        var spy = new SpyHotReloadWatcher();
        ctx.AppCommands.HotReloadWatcher = spy;
        ctx.ProjectManager.FileName = null;

        var chain = new AnimationChainSave { Name = "Walk" };
        chain.Frames.Add(TestHelpers.MakeFrame(@"C:\Sprites\hero.png"));
        ctx.Acls.AnimationChains.Add(chain);

        ctx.AppCommands.SyncHotReloadWatcher();

        Assert.NotNull(spy.LastStartPngPaths);
        Assert.Contains(@"C:\Sprites\hero.png", spy.LastStartPngPaths!,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void SyncHotReloadWatcher_UnsavedProjectNoPngs_StartsWithEmptyPngList()
    {
        var ctx = TestHelpers.SetupFreshAcls();
        var spy = new SpyHotReloadWatcher();
        ctx.AppCommands.HotReloadWatcher = spy;
        ctx.ProjectManager.FileName = null;

        ctx.Acls.AnimationChains.Add(new AnimationChainSave { Name = "Idle" });

        ctx.AppCommands.SyncHotReloadWatcher();

        Assert.NotNull(spy.LastStartPngPaths);
        Assert.Empty(spy.LastStartPngPaths!);
    }

    // ── SyncHotReloadWatcher – saved project ─────────────────────────────────

    [Fact]
    public void SyncHotReloadWatcher_SavedProject_PassesAchxPath()
    {
        var ctx = TestHelpers.SetupFreshAcls();
        var spy = new SpyHotReloadWatcher();
        ctx.AppCommands.HotReloadWatcher = spy;
        ctx.ProjectManager.FileName = @"C:\proj\anim.achx";

        ctx.AppCommands.SyncHotReloadWatcher();

        Assert.Equal(@"C:\proj\anim.achx", spy.LastStartAchxPath);
    }

    [Fact]
    public void SyncHotReloadWatcher_SavedProjectWithRelativePng_ResolvesAbsolutePath()
    {
        var ctx = TestHelpers.SetupFreshAcls();
        var spy = new SpyHotReloadWatcher();
        ctx.AppCommands.HotReloadWatcher = spy;
        ctx.ProjectManager.FileName = @"C:\proj\anim.achx";

        var chain = new AnimationChainSave { Name = "Run" };
        chain.Frames.Add(TestHelpers.MakeFrame("sprites/run.png"));
        ctx.Acls.AnimationChains.Add(chain);

        ctx.AppCommands.SyncHotReloadWatcher();

        Assert.NotNull(spy.LastStartPngPaths);
        var expectedAbs = System.IO.Path.Combine(@"C:\proj", "sprites/run.png");
        Assert.Contains(expectedAbs, spy.LastStartPngPaths!,
            StringComparer.OrdinalIgnoreCase);
    }

    // ── Spy ───────────────────────────────────────────────────────────────────

    private sealed class SpyHotReloadWatcher : IHotReloadWatcher
    {
        public string? LastStartAchxPath;
        public List<string>? LastStartPngPaths;

        public event Action<string>? AchxChangedOnDisk { add { } remove { } }
        public event Action<string>? PngChangedOnDisk { add { } remove { } }
        public event Action<string>? AchxDeletedOnDisk { add { } remove { } }

        public bool IsEnabled { get; set; } = true;

        public void StartWatching(string achxPath, IEnumerable<string> pngPaths)
        {
            LastStartAchxPath = achxPath;
            LastStartPngPaths = new List<string>(pngPaths);
        }

        public void UpdatePngList(IEnumerable<string> newPngPaths) { }
        public void StopWatching() { }
        public void RecordOwnSave(string filePath) { }
        public void Dispose() { }
    }
}
