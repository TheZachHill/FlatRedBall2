using System.IO;

namespace AnimationEditor.DocScreenshots;

/// <summary>
/// Resolves repo-relative folders from a runtime directory so the doc-screenshot regenerator can
/// write committed PNGs into the tracked <c>docs/</c> tree without a hard-coded absolute path.
/// </summary>
internal static class RepoLayout
{
    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> until it finds a <c>docs/SUMMARY.md</c>
    /// (the GitBook table-of-contents marker that identifies the published docs root), returning
    /// that <c>docs</c> folder's full path — or <c>null</c> if none is found up to the filesystem
    /// root. Keyed on <c>SUMMARY.md</c> specifically so the tool's own <c>docs/</c> folder (dev
    /// notes, decision records — no <c>SUMMARY.md</c>) is skipped in favor of the repo-root site.
    /// </summary>
    public static string? FindDocsRoot(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var docs = Path.Combine(dir.FullName, "docs");
            if (File.Exists(Path.Combine(docs, "SUMMARY.md")))
                return docs;
        }
        return null;
    }
}
