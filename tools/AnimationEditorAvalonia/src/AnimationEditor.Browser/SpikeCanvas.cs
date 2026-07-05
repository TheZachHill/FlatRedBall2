using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AnimationEditor.Browser;

/// <summary>
/// #535 M1 spike: reproduces the exact rendering mechanism used by
/// AnimationEditor.App's WireframeControl/PreviewControl (ICustomDrawOperation +
/// ISkiaSharpApiLeaseFeature, branching on GrContext for GPU vs CPU) so the browser
/// backend's support for that path can be checked directly, without any of the
/// desktop-only surrounding app plumbing.
/// <para>
/// Uses <see cref="Console.WriteLine"/>, not <c>Debug.WriteLine</c>, deliberately:
/// on wasm the runtime routes Console output to the browser devtools console, while
/// Debug.WriteLine needs a TraceListener that isn't wired up here and would be silent.
/// </para>
/// </summary>
public sealed class SpikeCanvas : Control
{
    private sealed class DrawOp : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        public DrawOp(Rect bounds) => _bounds = bounds;

        public Rect Bounds => _bounds;
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }

        public void Render(ImmediateDrawingContext ctx)
        {
            Console.WriteLine($"[spike] DrawOp.Render called, bounds={_bounds}");
            var feature = ctx.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            Console.WriteLine($"[spike] ISkiaSharpApiLeaseFeature = {(feature is null ? "null" : "present")}");
            var lease = feature?.Lease();
            if (lease is null)
            {
                // No Skia lease feature at all on this backend -- the biggest possible failure mode.
                Console.WriteLine("[spike] lease is null, aborting render");
                return;
            }

            using (lease)
            {
                var canvas = lease.SkCanvas;
                bool isGpu = lease.GrContext != null;
                Console.WriteLine($"[spike] got SkCanvas, isGpu={isGpu}");

                canvas.Clear(new SKColor(24, 28, 36));

                using var fill = new SKPaint { Color = new SKColor(80, 160, 255, 200), IsAntialias = true };
                canvas.DrawRect(new SKRect(20, 20, 220, 140), fill);

                using var circle = new SKPaint { Color = new SKColor(255, 140, 0, 220), IsAntialias = true };
                canvas.DrawCircle(300, 80, 60, circle);

                using var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
                using var font = new SKFont { Size = 20 };
                canvas.DrawText($"#535 spike -- backend: {(isGpu ? "GPU (ANGLE)" : "CPU (software)")}",
                    20, 180, font, textPaint);
                canvas.DrawText("If you can read this, ICustomDrawOperation + SkiaSharp render on Avalonia.Browser.",
                    20, 210, font, textPaint);
            }
        }
    }

    public SpikeCanvas()
    {
        Console.WriteLine("[spike] SpikeCanvas constructed");
    }

    public override void Render(DrawingContext ctx)
    {
        Console.WriteLine($"[spike] SpikeCanvas.Render (Control-level), Bounds={Bounds}");
        ctx.Custom(new DrawOp(Bounds));
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Console.WriteLine($"[spike] OnSizeChanged: {e.NewSize}");
        InvalidateVisual();
    }
}
