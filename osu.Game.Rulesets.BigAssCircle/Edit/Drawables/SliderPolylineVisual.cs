using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// The editor's slider representation: a polyline joining the head to each control-point node in
/// (angle → x, time → y) space, with a dot at every node. Node x offsets use the raw (unwrapped)
/// <see cref="BacPathControlPoint.RotationOffset"/>, so a path may extend into the ghost bands.
///
/// Vertices are recomputed each frame (the scroll scale can change with timeline zoom) but only pushed
/// to the <see cref="SmoothPath"/> when they actually changed.
/// </summary>
internal partial class SliderPolylineVisual : CompositeDrawable
{
    private readonly SliderBody slider;
    private readonly SmoothPath path;
    private readonly Container<Circle> nodeMarkers;

    private readonly List<Vector2> vertices = new List<Vector2>();

    [Resolved]
    private Playfield playfield { get; set; } = null!;

    public SliderPolylineVisual(SliderBody slider)
    {
        this.slider = slider;
        RelativeSizeAxes = Axes.Both;

        InternalChildren = new Drawable[]
        {
            path = new SmoothPath { PathRadius = 3 },
            nodeMarkers = new Container<Circle> { RelativeSizeAxes = Axes.Both },
        };

        Colour = slider.Side == Core.HorizontalDirection.Left ? Constants.LeftColour : Constants.RightColour;
    }

    protected override void Update()
    {
        base.Update();

        var newVertices = computeVertices();

        if (vertexListEquals(newVertices))
            return;

        vertices.Clear();
        vertices.AddRange(newVertices);

        path.Vertices = vertices;
        // Path auto-sizes to its vertex bounds; undo the bounding-box offset so vertex coordinates land
        // in our local space (same idiom as the gameplay DrawableSliderBody).
        path.Position = -path.PositionInBoundingBox(Vector2.Zero);

        nodeMarkers.Clear();

        foreach (var v in vertices)
        {
            nodeMarkers.Add(new Circle
            {
                Size = new Vector2(10),
                Origin = Anchor.Centre,
                Position = v,
            });
        }
    }

    private List<Vector2> computeVertices()
    {
        var result = new List<Vector2>();

        double duration = slider.Duration;
        if (duration <= 0)
            return result;

        float pxPerDeg = playfield.DrawWidth / EditorAngleMapping.TOTAL_DEGREES;
        float centreX = DrawWidth / 2;

        // head at the bottom (start time), nodes rising toward the end time.
        result.Add(new Vector2(centreX, DrawHeight));

        foreach (var cp in slider.Path.ControlPoints)
        {
            result.Add(new Vector2(
                centreX + cp.RotationOffset * pxPerDeg,
                DrawHeight * (float)(1 - cp.TimeOffset / duration)));
        }

        return result;
    }

    private bool vertexListEquals(List<Vector2> other)
    {
        if (vertices.Count != other.Count)
            return false;

        for (int i = 0; i < vertices.Count; i++)
        {
            if (vertices[i] != other[i])
                return false;
        }

        return true;
    }
}
