using System;
using System.IO;
using Xunit;

namespace AnimationEditor.DocScreenshots;

public class RepoLayoutTests
{
    [Fact]
    public void FindDocsRoot_WalksUpToDirectoryContainingDocsSummary()
    {
        var temp = Path.Combine(Path.GetTempPath(), "AEDocRootTest", Guid.NewGuid().ToString("N"));
        var docs = Path.Combine(temp, "docs");
        var deepInside = Path.Combine(temp, "tools", "x", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(docs);
        Directory.CreateDirectory(deepInside);
        File.WriteAllText(Path.Combine(docs, "SUMMARY.md"), "# Table of contents");
        try
        {
            Assert.Equal(docs, RepoLayout.FindDocsRoot(deepInside));
        }
        finally { Directory.Delete(temp, recursive: true); }
    }

    [Fact]
    public void FindDocsRoot_ReturnsNull_WhenNoDocsFolderAbove()
    {
        var temp = Path.Combine(Path.GetTempPath(), "AEDocRootTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            Assert.Null(RepoLayout.FindDocsRoot(temp));
        }
        finally { Directory.Delete(temp, recursive: true); }
    }
}
