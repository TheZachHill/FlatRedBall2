using System;
using System.Collections.Generic;
using AnimationEditor.Core.IO;
using Xunit;

namespace AnimationEditor.Core.Tests;

// #535 M3 follow-up: the browser build has no FileSystemWatcher, so a live "Files" panel has to
// poll IStorageFile.GetBasicPropertiesAsync() (Size/DateModified) instead. FindChanged is the
// pure comparison at the heart of that poll -- kept free of IStorageFolder/Avalonia so it's
// testable without a browser.
public class FolderSnapshotDiffTests
{
    private static FolderEntrySnapshot Entry(ulong? size, DateTimeOffset? modified) => new(size, modified);

    [Fact]
    public void FindChanged_EmptyPreviousAndEmptyCurrent_ReturnsEmpty()
    {
        var previous = new Dictionary<string, FolderEntrySnapshot>();
        var current = new Dictionary<string, FolderEntrySnapshot>();

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Empty(changed);
    }

    [Fact]
    public void FindChanged_NewFileNotInPrevious_IsNotReportedAsChanged()
    {
        // A file appearing for the first time is a new file, not an edit -- BrowserFolderWatcher's
        // seed pass already recorded it, so it must not also show up in the change list.
        var previous = new Dictionary<string, FolderEntrySnapshot>();
        var current = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Empty(changed);
    }

    [Fact]
    public void FindChanged_SizeDiffers_ReportsFile()
    {
        var previous = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };
        var current = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(200, DateTimeOffset.UnixEpoch) };

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Equal(["a.png"], changed);
    }

    [Fact]
    public void FindChanged_DateModifiedDiffers_ReportsFile()
    {
        var previous = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };
        var current = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch.AddSeconds(1)) };

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Equal(["a.png"], changed);
    }

    [Fact]
    public void FindChanged_IdenticalSizeAndDateModified_ReportsNothing()
    {
        var previous = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };
        var current = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Empty(changed);
    }

    [Fact]
    public void FindChanged_FileRemovedFromCurrent_IsNotReportedAsChanged()
    {
        // Deletion isn't a texture edit the preview needs to re-decode; BrowserFolderWatcher
        // only cares about content changes to files it can still read.
        var previous = new Dictionary<string, FolderEntrySnapshot> { ["a.png"] = Entry(100, DateTimeOffset.UnixEpoch) };
        var current = new Dictionary<string, FolderEntrySnapshot>();

        var changed = FolderSnapshotDiff.FindChanged(previous, current);

        Assert.Empty(changed);
    }
}
