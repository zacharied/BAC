using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.BigAssCircle.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

/// <summary>
/// Draws a <see cref="BacPath"/> as a connected polyline in the playfield's polar coordinate system.
///
/// The path is made up of a start node (which fixes the initial direction and time) followed by a
/// number of child control points. Every node maps to a point <c>(θ, r)</c> where <c>θ</c> is the
/// node's angle and <c>r</c> is its distance from the centre. The radius is driven by the scrolling
/// algorithm, so as time advances every node "comes out" from the centre towards the surrounding arc,
/// giving the whole path the look of a durationed object emerging from the middle of the screen.
/// </summary>
public partial class DrawableBacPath : DrawableHitObject<BacHitObject>
{
    // Typed accessor over the base BacHitObject, mirroring DrawableSlider.HitObject.
    // Retyping the drawable to the base BacHitObject (rather than BacPathStartHitObject) is what
    // lets DrawableBigAssCircleRuleset.CreateDrawableRepresentation return it as a
    // DrawableHitObject<BacHitObject> — generic drawables are invariant.
    public new BacPathStartHitObject HitObject => (BacPathStartHitObject)base.HitObject;

    /// <summary>
    /// Thickness of the rendered line, in pixels.
    /// </summary>
    public float Thickness { get; init; } = 5;

    /// <summary>
    /// Number of straight sub-segments used to approximate the link between two consecutive nodes.
    /// Because interpolation happens in polar space, a link whose endpoints share a radius renders as
    /// an arc rather than a chord; more sub-segments make that arc smoother.
    /// </summary>
    private const int segments_per_link = 8;

    [Resolved]
    private BigAssCircleScrollingHitObjectContainer scrollingContainer { get; set; }

    private readonly Container<Box> pathLinesContainer = new()
    {
        RelativeSizeAxes = Axes.Both,
    };

    private readonly List<Box> boxes = new();

    // Nested child hit objects live here so they receive a clock and are updated/judged like any
    // other DrawableHitObject. They draw nothing (the path visuals come entirely from
    // pathLinesContainer); without a real parent in the tree, OnKilled would dereference a null clock.
    private readonly Container<DrawableBacPathChild> nestedContainer = new()
    {
        RelativeSizeAxes = Axes.Both,
    };

    // Per-node data, rebuilt whenever a new hit object is applied.
    private float[] nodeRadians = Array.Empty<float>();
    private double[] nodeTimes = Array.Empty<double>();
    private float[] nodeRadii = Array.Empty<float>();

    public DrawableBacPath(BacPathStartHitObject hitObject = null)
        : base(hitObject)
    {
        // Fill the container so our local space matches it 1:1: the centre is DrawSize / 2 and radii
        // returned by the container map directly onto our own coordinate space.
        RelativeSizeAxes = Axes.Both;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        AddInternal(pathLinesContainer);
        AddInternal(nestedContainer);
    }

    protected override void OnApply()
    {
        base.OnApply();
        rebuildNodes();
    }

    protected override void PrepareForUse()
    {
        base.PrepareForUse();
        pathLinesContainer.FadeInFromZero(100, Easing.In);
    }

    protected override void Update()
    {
        base.Update();
        updatePath();
    }

    /// <summary>
    /// Precomputes the constant angle/time of every node from the applied hit object.
    /// </summary>
    private void rebuildNodes()
    {
        var start = HitObject;
        int count = 1 + start.Path.ControlPoints.Count;

        nodeRadians = new float[count];
        nodeTimes = new double[count];
        nodeRadii = new float[count];

        // Node 0 is the start node itself.
        nodeRadians[0] = toRadians(start.DirectionDeg);
        nodeTimes[0] = start.StartTime;

        if (start.Path == null)
            return;

        int i = 1;

        foreach (var controlPoint in start.Path.ControlPoints)
        {
            nodeRadians[i] = toRadians(start.DirectionDeg + controlPoint.RotationOffset);
            nodeTimes[i] = start.StartTime + controlPoint.TimeOffset;
            i++;
        }
    }

    /// <summary>
    /// Recomputes the line geometry for the current frame. Each node's radius is resolved through the
    /// scrolling algorithm, then consecutive nodes are joined by sub-segmented boxes interpolated in
    /// polar space.
    /// </summary>
    private void updatePath()
    {
        int visible = 0;

        if (nodeTimes.Length >= 2)
        {
            var centre = DrawSize / 2;

            for (int i = 0; i < nodeTimes.Length; i++)
                // Clamp to the centre. A node whose time is still more than one time-range in the
                // future has negative progress; pinning it at radius 0 keeps it at screen centre so
                // the line visibly emerges outward as each node crosses the centre, instead of being
                // drawn on the opposite side. This is what gives the path its "comes out of the
                // middle" look, matching the button hit objects.
                nodeRadii[i] = MathF.Max(0f, scrollingContainer.ProgressAtTime(nodeTimes[i]));

            for (int i = 0; i < nodeTimes.Length - 1; i++)
            {
                float thetaA = nodeRadians[i];
                float thetaB = nodeRadians[i + 1];
                float rA = nodeRadii[i];
                float rB = nodeRadii[i + 1];

                Vector2 previous = centre + polarToCartesian(thetaA, rA);

                for (int k = 1; k <= segments_per_link; k++)
                {
                    float t = (float)k / segments_per_link;
                    Vector2 next = centre + polarToCartesian(lerp(thetaA, thetaB, t), lerp(rA, rB, t));

                    updateSegment(getBox(visible++), previous, next);

                    previous = next;
                }
            }
        }

        // Hide boxes that were used by a longer path on a previous frame / hit object.
        for (int i = visible; i < boxes.Count; i++)
            boxes[i].Alpha = 0;
    }

    private void updateSegment(Box box, Vector2 start, Vector2 end)
    {
        Vector2 diff = end - start;
        float length = diff.Length;

        box.Alpha = length > 0 && Thickness > 0 ? 1 : 0;
        box.Position = start;
        box.Size = new Vector2(length, Thickness);
        box.Rotation = MathF.Atan2(diff.Y, diff.X) * 180f / MathF.PI;
    }

    /// <summary>
    /// Lazily grows the box pool, returning the box at <paramref name="index"/>.
    /// </summary>
    private Box getBox(int index)
    {
        while (boxes.Count <= index)
        {
            var box = new Box
            {
                Origin = Anchor.CentreLeft,
                Colour = Color4.White,
                Alpha = 0,
            };

            boxes.Add(box);
            pathLinesContainer.Add(box);
        }

        return boxes[index];
    }

    protected override void CheckForResult(bool userTriggered, double timeOffset)
    {
        // The path is fully emerged once every node has reached the arc, i.e. at its end time.
        if (Time.Current >= HitObject.EndTime)
            // todo: implement judgement logic
            ApplyMaxResult();
    }

    protected override void UpdateHitStateTransforms(ArmedState state)
    {
        const double duration = 1000;

        switch (state)
        {
            case ArmedState.Hit:
                pathLinesContainer.FadeOut(350, Easing.OutQuint).OnComplete(_ => Expire());
                break;

            case ArmedState.Miss:
                pathLinesContainer.FadeColour(Color4.Red, duration);
                pathLinesContainer.FadeOut(duration, Easing.InQuint).OnComplete(_ => Expire());
                break;
        }
    }

    private static Vector2 polarToCartesian(float radians, float radius)
        => new Vector2(MathF.Sin(radians) * radius, MathF.Cos(radians) * radius);

    private static float toRadians(float degrees) => degrees * MathF.PI / 180f;

    private static float lerp(float a, float b, float t) => a + (b - a) * t;

    protected override DrawableHitObject CreateNestedHitObject(HitObject hitObject)
    {
        if (hitObject is BacPathChildHitObject child)
        {
            return new DrawableBacPathChild(child);
        }

        throw new InvalidOperationException($"cannot create nested hit object for type {hitObject.GetType().Name}");
    }

    protected override void AddNestedHitObject(DrawableHitObject hitObject)
    {
        base.AddNestedHitObject(hitObject);

        if (hitObject is DrawableBacPathChild child)
            nestedContainer.Add(child);
    }

    protected override void ClearNestedHitObjects()
    {
        base.ClearNestedHitObjects();
        nestedContainer.Clear(false);
    }
}
