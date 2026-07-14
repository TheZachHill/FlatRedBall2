namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Computes how much the fine (minor) grid lines should fade as the camera zooms out.
/// Below <see cref="FadeEndZoom"/> the fine grid is fully invisible, leaving only the
/// coarse (major) lines — dense minor lines look like noise at low zoom (#720).
/// </summary>
public static class GridFadeCalculator
{
    /// <summary>Zoom at/above which the fine grid renders at full opacity.</summary>
    public const float FadeStartZoom = 0.5f;

    /// <summary>Zoom at/below which the fine grid is fully invisible.</summary>
    public const float FadeEndZoom = 0.25f;

    /// <summary>
    /// Returns the fine-grid opacity multiplier (0..1) for the given zoom: 1 at/above
    /// <see cref="FadeStartZoom"/>, 0 at/below <see cref="FadeEndZoom"/>, linearly
    /// interpolated in between.
    /// </summary>
    public static float MinorLineAlphaFactor(float zoom)
    {
        if (zoom >= FadeStartZoom) return 1f;
        if (zoom <= FadeEndZoom) return 0f;
        return (zoom - FadeEndZoom) / (FadeStartZoom - FadeEndZoom);
    }
}
