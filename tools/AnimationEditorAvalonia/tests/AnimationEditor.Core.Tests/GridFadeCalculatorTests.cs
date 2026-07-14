using AnimationEditor.Core.Rendering;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class GridFadeCalculatorTests
{
    [Fact]
    public void MinorLineAlphaFactor_AtOrAboveFadeStartZoom_ReturnsOne()
    {
        Assert.Equal(1f, GridFadeCalculator.MinorLineAlphaFactor(GridFadeCalculator.FadeStartZoom));
        Assert.Equal(1f, GridFadeCalculator.MinorLineAlphaFactor(1f));
        Assert.Equal(1f, GridFadeCalculator.MinorLineAlphaFactor(4f));
    }

    [Fact]
    public void MinorLineAlphaFactor_AtOrBelowFadeEndZoom_ReturnsZero()
    {
        Assert.Equal(0f, GridFadeCalculator.MinorLineAlphaFactor(GridFadeCalculator.FadeEndZoom));
        Assert.Equal(0f, GridFadeCalculator.MinorLineAlphaFactor(0.1f));
        Assert.Equal(0f, GridFadeCalculator.MinorLineAlphaFactor(0f));
    }

    [Fact]
    public void MinorLineAlphaFactor_BetweenThresholds_InterpolatesLinearly()
    {
        // Midpoint between FadeEndZoom (0.25) and FadeStartZoom (0.5) is 0.375 → factor 0.5.
        float midZoom = (GridFadeCalculator.FadeStartZoom + GridFadeCalculator.FadeEndZoom) / 2f;
        Assert.Equal(0.5f, GridFadeCalculator.MinorLineAlphaFactor(midZoom), precision: 4);
    }
}
