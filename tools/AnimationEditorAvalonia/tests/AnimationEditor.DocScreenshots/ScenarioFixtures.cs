using System;
using System.IO;

namespace AnimationEditor.DocScreenshots;

/// <summary>
/// Stages the real <c>characters.png</c> sprite sheet (bundled beside the test exe, see the
/// project's fixture <c>None</c> include) into a throwaway temp project so scenarios can build
/// state that loads a genuine texture — per the <c>animation-editor-screenshots</c> skill's
/// "use real texture fixtures, not a generated placeholder" rule.
/// </summary>
internal static class ScenarioFixtures
{
    /// <summary>The bundled 736×128 character sheet: a 32×32 grid, row 1 (y=32) a knight walk cycle.</summary>
    public static string CharacterSheetSource =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "characters.png");

    public const int SheetWidth = 736;
    public const int SheetHeight = 128;
    public const int CellSize = 32;

    /// <summary>
    /// Copies the character sheet into a fresh temp folder and points
    /// <see cref="AnimationEditor.Core.ProjectManager.FileName"/> at a <c>demo.achx</c> beside it.
    /// Both are needed: selecting a frame resolves its relative <c>characters.png</c> texture
    /// against the project folder (via <c>SyncTextureCombo</c>), and every <c>AddFrame</c>/
    /// <c>AddChain</c> auto-saves the <c>.achx</c> to that folder — so it must be writable temp,
    /// never the tracked fixture directory.
    /// </summary>
    public static void UseCharacterSheetProject(TestServices ctx)
    {
        var dir = Path.Combine(Path.GetTempPath(), "AEDocShots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.Copy(CharacterSheetSource, Path.Combine(dir, "characters.png"), overwrite: true);
        ctx.ProjectManager.FileName = Path.Combine(dir, "demo.achx");
    }
}
