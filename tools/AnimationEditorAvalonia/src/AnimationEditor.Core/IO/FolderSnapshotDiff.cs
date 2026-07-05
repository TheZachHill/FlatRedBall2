using System;
using System.Collections.Generic;

namespace AnimationEditor.Core.IO;

/// <summary>
/// A file's size and last-modified time, cheap enough to poll without reading its content.
/// Used to detect edits to a folder when no live filesystem-change notification is available
/// (e.g. the browser build, which has no <c>FileSystemWatcher</c> equivalent).
/// </summary>
public readonly record struct FolderEntrySnapshot(ulong? Size, DateTimeOffset? Modified);

/// <summary>
/// Pure comparison between two folder snapshots (<see cref="FolderEntrySnapshot"/> keyed by file
/// name), used to detect which files were edited between polls. Deliberately has no dependency on
/// how the snapshot was taken (disk <c>FileInfo</c>, browser <c>IStorageFile.GetBasicPropertiesAsync</c>,
/// or anything else) so it's usable, and testable, from any polling source.
/// </summary>
public static class FolderSnapshotDiff
{
    /// <summary>
    /// Returns the names present in both <paramref name="previous"/> and <paramref name="current"/>
    /// whose <see cref="FolderEntrySnapshot"/> differs. A name only in <paramref name="current"/>
    /// (a new file) or only in <paramref name="previous"/> (a deleted file) is not reported --
    /// callers that need to react to new/removed files should diff the key sets themselves.
    /// </summary>
    public static IReadOnlyList<string> FindChanged(
        IReadOnlyDictionary<string, FolderEntrySnapshot> previous,
        IReadOnlyDictionary<string, FolderEntrySnapshot> current)
    {
        var changed = new List<string>();
        foreach (var (name, currentEntry) in current)
        {
            if (previous.TryGetValue(name, out var previousEntry) && previousEntry != currentEntry)
                changed.Add(name);
        }
        return changed;
    }
}
