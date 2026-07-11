using System;
using System.Diagnostics;
using System.IO;
using Shouldly;
using Xunit;

namespace FlatRedBall2.Tests.Automation;

// Issue #683. Published NuGet packages are packed with `dotnet pack -c Release`, so a build-time
// `#if DEBUG` around AutomationMode strips the real implementation out of every shipped assembly —
// EnableAutomationMode() is a permanent no-op for NuGet consumers regardless of their own project's
// build configuration. The fix keeps the implementation compiled into the engine assembly under
// both configurations and instead gates the public entry point with [Conditional("DEBUG")], which
// the C# compiler evaluates against the *caller's* DEBUG symbol at each call site.
//
// This builds the real engine assembly in Release and inspects its bytes for the internal
// StartAutomationMode symbol, which previously only existed in a Debug-configured build of the
// engine itself.
public class AutomationModeReleaseBuildTests
{
    [Fact]
    public void ReleaseConfigurationBuild_OfEngineItself_StillContainsAutomationModeImplementation()
    {
        var repoRoot = Packaging.TemplatePackageReferenceTests.RepoRootForTests;
        var csprojPath = Path.Combine(repoRoot, "src", "FlatRedBall2.csproj");

        var outputDir = Path.Combine(Path.GetTempPath(), "frb2-automation-relcheck-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);
        try
        {
            var startInfo = new ProcessStartInfo(
                "dotnet",
                $"build \"{csprojPath}\" -c Release -f net10.0 -o \"{outputDir}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo)!;
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            process.ExitCode.ShouldBe(0, $"Release build of FlatRedBall2.csproj failed:{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");

            var dllBytes = File.ReadAllBytes(Path.Combine(outputDir, "FlatRedBall2.dll"));
            var dllText = System.Text.Encoding.Latin1.GetString(dllBytes);

            dllText.Contains("StartAutomationMode").ShouldBeTrue(
                "AutomationMode's real implementation must be compiled into the engine assembly in " +
                "Release too — the gate belongs at the EnableAutomationMode call site (via " +
                "[Conditional(\"DEBUG\")]), evaluated against the consumer's DEBUG symbol, not the " +
                "engine's own build configuration.");
        }
        finally
        {
            try { Directory.Delete(outputDir, recursive: true); }
            catch { /* best-effort temp cleanup */ }
        }
    }
}
