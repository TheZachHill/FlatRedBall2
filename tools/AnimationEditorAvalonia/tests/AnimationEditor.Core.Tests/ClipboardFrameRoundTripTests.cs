using AnimationEditor.Core.IO;
using FlatRedBall2.Animation.Content;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AnimationEditor.Core.Tests;

// Diagnostic: does a frame with an initialized/shape-bearing ShapesSave round-trip
// through the clipboard serializer? Real editor frames always have ShapesSave set
// (AddFrame initializes it), unlike the existing shapeless paste test.
public class ClipboardFrameRoundTripTests
{
    [Fact]
    public void Frame_WithNullShapes_RoundTrips()
    {
        var xml = ClipboardPayload.Serialize(new List<AnimationFrameSave>
            { new() { TextureName = "a.png" } });
        Assert.True(ClipboardPayload.TryDeserialize(xml, out _, out var frames, out _, out _));
        Assert.Single(frames!);
    }

    [Fact]
    public void Frame_WithEmptyShapesSave_RoundTrips()
    {
        var xml = ClipboardPayload.Serialize(new List<AnimationFrameSave>
            { new() { TextureName = "a.png", ShapesSave = new ShapesSave() } });
        Assert.True(ClipboardPayload.TryDeserialize(xml, out _, out var frames, out _, out _));
        Assert.Single(frames!);
    }

    [Fact]
    public void Frame_WithRectangle_RoundTrips()
    {
        var frame = new AnimationFrameSave { TextureName = "a.png", ShapesSave = new ShapesSave() };
        frame.ShapesSave.Shapes.Add(new AARectSave { Name = "Hit", ScaleX = 4, ScaleY = 4 });

        var xml = ClipboardPayload.Serialize(new List<AnimationFrameSave> { frame });
        Assert.True(ClipboardPayload.TryDeserialize(xml, out _, out var frames, out _, out _));
        Assert.Single(frames!);
        Assert.Single(frames![0].ShapesSave!.AARectSaves);
    }

    [Fact]
    public void Chain_WithShapedFrame_RoundTrips()
    {
        var chain = new AnimationChainSave { Name = "Walk" };
        var frame = new AnimationFrameSave { TextureName = "a.png", ShapesSave = new ShapesSave() };
        frame.ShapesSave.Shapes.Add(new CircleSave { Name = "Hurt", Radius = 3 });
        chain.Frames.Add(frame);

        var xml = ClipboardPayload.Serialize(new List<AnimationChainSave> { chain });
        Assert.True(ClipboardPayload.TryDeserialize(xml, out var chains, out _, out _, out _));
        Assert.Single(chains!);
        Assert.Single(chains![0].Frames[0].ShapesSave!.CircleSaves);
    }
}
