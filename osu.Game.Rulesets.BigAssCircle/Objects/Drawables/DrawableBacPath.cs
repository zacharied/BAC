using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Lines;
using osu.Game.Rulesets.BigAssCircle.UI;
using osu.Framework.Utils;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

/// <summary>
/// Draws a <see cref="BacPath"/> as a connected polyline in the playfield's polar coordinate system.
///
/// The path is made up of a start node (which fixes the initial direction and time) followed by a
/// number of child control points. Every node maps to a point <c>(θ, r)</c> where <c>θ</c> is the
/// node's angle and <c>r</c> is its distance from the centre. The radius is driven by the scrolling
/// algorithm, so as time advances every node "comes out" from the centre towards the surrounding arc,
/// giving the whole path the look of a durationed object emerging from the middle of the screen.
///
/// Rendering is delegated to <see cref="SmoothPath"/> (osu!framework's line renderer), which handles
/// thickness, rounded joints and anti-aliasing for us. This drawable's job is purely to produce the
/// list of cartesian vertices each frame — subdividing every link in polar space so constant-radius
/// links render as arcs, and clipping each link to the visible band <c>[0, ScrollLength]</c>.
/// </summary>
public partial class DrawableBacPath : DrawableHitObject<BacHitObject>
{
    // Typed accessor over the base BacHitObject, mirroring DrawableSlider.HitObject.
    // Retyping the drawable to the base BacHitObject (rather than BacPathStartHitObject) is what
    // lets DrawableBigAssCircleRuleset.CreateDrawableRepresentation return it as a
    // DrawableHitObject<BacHitObject> — generic drawables are invariant.
    public new BacPathStartHitObject HitObject => (BacPathStartHitObject)base.HitObject;

    /// <summary>
    /// Full width of the rendered line, in pixels. Half of this becomes the <see cref="Path.PathRadius"/>.
    /// </summary>
    public float Thickness { get; init; } = 15;

    /// <summary>
    /// Colour of the additive glow rendered behind the path. White gives a neutral halo that brightens
    /// whatever colour the path itself is tinted.
    /// </summary>
    public ColourInfo GlowColour { get; init; } = Colour4.White;

    /// <summary>
    /// Blur radius (sigma, in pixels) of the glow — larger values spread the halo further out.
    /// </summary>
    public float GlowBlurSigma { get; init; } = 30f;

    /// <summary>
    /// Intensity multiplier applied to the glow.
    /// </summary>
    public float GlowStrength { get; init; } = 30f;

    /// <summary>
    /// Number of straight sub-segments used to approximate the link between two consecutive nodes.
    /// Because interpolation happens in polar space, a link whose endpoints share a radius renders as
    /// an arc rather than a chord; more sub-segments make that arc smoother.
    /// </summary>
    private const int segments_per_link = 12;

    [Resolved]
    private BigAssCircleScrollingHitObjectContainer scrollingContainer { get; set; } = null!;

    // Tinted/faded as a unit (fade-in, red-on-miss). SmoothPath forces its own draw colour to white,
    // so colour/alpha applied here is what tints the path via the framebuffer blit.
    private readonly Container<SmoothPath> pathContainer = new()
    {
        RelativeSizeAxes = Axes.Both,
    };

    // Pool of SmoothPaths. Normally the visible portion of the path is a single contiguous run, but if
    // the run breaks (e.g. non-monotonic node times leave a gap) we start a fresh path so the two spans
    // are not joined by a stray line. Grown lazily, mirroring the old box pool.
    private readonly List<SmoothPath> paths = new();

    // Nested child hit objects live here so they receive a clock and are updated/judged like any
    // other DrawableHitObject. They draw nothing (the path visuals come entirely from pathContainer);
    // without a real parent in the tree, OnKilled would dereference a null clock.
    private readonly Container<DrawableBacPathChild> nestedContainer = new()
    {
        RelativeSizeAxes = Axes.Both,
    };

    // Per-node data, rebuilt whenever a new hit object is applied.
    private float[] nodeRadians = Array.Empty<float>();
    private double[] nodeTimes = Array.Empty<double>();
    private float[] nodeRadii = Array.Empty<float>();

    // Angular sweep rate (dθ/dtime, radians per ms) at each node — the Catmull-Rom tangents used to
    // smooth the angle interpolation so the sweep velocity is continuous through nodes.
    private float[] nodeThetaSlopes = Array.Empty<float>();

    // Per-link interpolation, taken from the control point at each link's end node (a control point
    // governs the segment leading into it). Off / Easing.None by default, so a link keeps exact linear
    // geometry unless its control point opts in. Indexed by link i = node[i] -> node[i + 1].
    private bool[] linkSmooth = Array.Empty<bool>();
    private Easing[] linkEasing = Array.Empty<Easing>();

    // Reused each frame to accumulate the vertices of the contiguous run currently being built.
    private readonly List<Vector2> scratchVertices = new();

    public DrawableBacPath(BacPathStartHitObject? hitObject = null)
        : base(hitObject)
    {
        RelativeSizeAxes = Axes.Both;
        pathContainer.Colour = HitObject.Side == HorizontalDirection.Left ? Constants.LeftColour : Constants.RightColour;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        // Wrap the whole path pool in an additive glow. GlowEffect buffers pathContainer's rendered
        // alpha and blurs it, so the halo hugs the actual line shape (not a bounding box) and a single
        // buffer covers every pooled path. The crisp paths are drawn in front of the halo. Fading or
        // recolouring pathContainer flows through to the glow, since it is regenerated from that content.
        AddInternal(new GlowEffect
        {
            Colour = GlowColour,
            BlurSigma = new Vector2(GlowBlurSigma),
            Strength = GlowStrength,
            // PadExtent would inset (and misalign) the relatively-sized pathContainer, so leave it off.
            PadExtent = false,
        }.ApplyTo(pathContainer));

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
        pathContainer.FadeInFromZero(100, Easing.In);
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
        var controlPoints = start.Path.ControlPoints;
        int linkCount = controlPoints.Count;
        int count = 1 + linkCount;

        nodeRadians = new float[count];
        nodeTimes = new double[count];
        nodeRadii = new float[count];
        nodeThetaSlopes = new float[count];
        linkSmooth = new bool[linkCount];
        linkEasing = new Easing[linkCount];

        // Node 0 is the start node itself.
        nodeRadians[0] = toRadians(start.DirectionDeg);
        nodeTimes[0] = start.StartTime;

        for (int i = 0; i < linkCount; i++)
        {
            var controlPoint = controlPoints[i];

            nodeRadians[i + 1] = toRadians(start.DirectionDeg + controlPoint.RotationOffset);
            nodeTimes[i + 1] = start.StartTime + controlPoint.TimeOffset;

            // A control point governs the segment leading into it: link[i] ends at node[i + 1] = CP[i].
            linkSmooth[i] = controlPoint.Smooth;
            linkEasing[i] = controlPoint.SweepEasing;
        }

        // Catmull-Rom tangents: centred difference of angle over time for interior nodes, one-sided at
        // the ends (the Min/Max clamps collapse the difference to the single available neighbour).
        for (int n = 0; n < count; n++)
        {
            int lo = Math.Max(0, n - 1);
            int hi = Math.Min(count - 1, n + 1);
            double dt = nodeTimes[hi] - nodeTimes[lo];

            nodeThetaSlopes[n] = dt > 0 ? (float)((nodeRadians[hi] - nodeRadians[lo]) / dt) : 0f;
        }
    }

    /// <summary>
    /// Recomputes the line geometry for the current frame. Each node's radius is resolved through the
    /// scrolling algorithm, then the visible portion of the path is walked link-by-link and emitted as
    /// one or more <see cref="SmoothPath"/> vertex lists.
    /// </summary>
    private void updatePath()
    {
        int pathIndex = 0;

        if (nodeTimes.Length >= 2)
        {
            float ringRadius = scrollingContainer.ScrollLength;

            for (int i = 0; i < nodeTimes.Length; i++)
                // Raw, unclamped distance from the centre. Negative means the node has not yet emerged
                // from the centre; greater than the ring radius means it has already been consumed by
                // the outer edge. Both ends are handled by clipping each link to the visible band
                // below — NOT by clamping the node. Clamping a not-yet-emerged node to the centre would
                // draw the whole link from the emerged node straight to the centre at once, instead of
                // letting the curve creep outward from the middle a little at a time.
                nodeRadii[i] = scrollingContainer.DistanceFromCentreAtTime(nodeTimes[i]);

            scratchVertices.Clear();

            for (int i = 0; i < nodeTimes.Length - 1; i++)
            {
                float rA = nodeRadii[i];
                float rB = nodeRadii[i + 1];

                // Draw only the part of the link whose radius lies within [0, ring]. The inner crossing
                // (radius 0) is the emergence front creeping out from the centre; the outer crossing
                // (radius == ring) is where the curve is consumed by the edge. Radius varies linearly
                // along the link, so this is a plain 1-D clip of the parameter range.
                if (!clipToBand(rA, rB, ringRadius, out float tLo, out float tHi))
                {
                    // This link is entirely outside the visible band; the run cannot continue past it.
                    pathIndex = flushRun(pathIndex);
                    continue;
                }

                Vector2 startPoint = pointAt(i, rA, rB, tLo);

                // Continue the current run only if this link's visible portion begins exactly where the
                // last one ended (a shared, fully-visible node). Otherwise there is a gap — flush and
                // start a fresh path so the two spans are not bridged by a stray line.
                if (scratchVertices.Count > 0 && !approxEqual(scratchVertices[^1], startPoint))
                    pathIndex = flushRun(pathIndex);

                if (scratchVertices.Count == 0)
                    scratchVertices.Add(startPoint);

                for (int k = 1; k <= segments_per_link; k++)
                {
                    float t = tLo + (tHi - tLo) * ((float)k / segments_per_link);
                    scratchVertices.Add(pointAt(i, rA, rB, t));
                }

                // Clipped before reaching its end node: the curve is consumed by the ring (or dips back
                // below the centre) here, so this contiguous run ends.
                if (tHi < 1f)
                    pathIndex = flushRun(pathIndex);
            }

            pathIndex = flushRun(pathIndex);
        }

        // Hide paths that were used by a longer path on a previous frame / hit object.
        for (int i = pathIndex; i < paths.Count; i++)
            paths[i].Vertices = Array.Empty<Vector2>();
    }

    /// <summary>
    /// Commits the vertices accumulated in <see cref="scratchVertices"/> (if it forms a drawable run of
    /// at least two points) to the next pooled path, then clears the scratch buffer. Returns the updated
    /// index into <see cref="paths"/>.
    /// </summary>
    private int flushRun(int pathIndex)
    {
        if (scratchVertices.Count >= 2)
        {
            var path = getPath(pathIndex++);

            // Vertices setter copies the list, so reusing the scratch buffer afterwards is safe.
            path.Vertices = scratchVertices;

            // Path auto-sizes to its vertex bounds and offsets content by vertexBounds.TopLeft; undo that
            // offset so a vertex at the polar origin (0,0) lands on the playfield centre (our anchor).
            path.Position = -path.PositionInBoundingBox(Vector2.Zero);
        }

        scratchVertices.Clear();
        return pathIndex;
    }

    /// <summary>
    /// Lazily grows the path pool, returning the path at <paramref name="index"/>.
    /// </summary>
    private SmoothPath getPath(int index)
    {
        while (paths.Count <= index)
        {
            var path = new SmoothPath
            {
                // Anchor the polar origin (vertex 0,0) at the playfield centre. Position (set per frame)
                // compensates for the auto-size bounding-box offset.
                Anchor = Anchor.Centre,
                PathRadius = Thickness / 2,
            };

            paths.Add(path);
            pathContainer.Add(path);
        }

        return paths[index];
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
                pathContainer.FadeOut(350, Easing.OutQuint).OnComplete(_ => Expire());
                break;

            case ArmedState.Miss:
                pathContainer.FadeColour(Colour4.Red, duration);
                pathContainer.FadeOut(duration, Easing.InQuint).OnComplete(_ => Expire());
                break;
        }
    }

    private static Vector2 polarToCartesian(float radians, float radius)
        => new Vector2(MathF.Cos(radians) * radius, -MathF.Sin(radians) * radius);

    // Point at parameter t along a link, in polar-origin-centred space (vertex (0,0) is the centre).
    // Angle is smoothed/eased per the link's control point; radius stays linear so the time→radius
    // mapping the clip relies on is exact.
    private Vector2 pointAt(int link, float rA, float rB, float t)
        => polarToCartesian(thetaAt(link, t), lerp(rA, rB, t));

    /// <summary>
    /// Evaluates the smoothed angle at parameter <paramref name="t"/> (0..1) along the given
    /// <paramref name="link"/>, using cubic Hermite interpolation with the precomputed Catmull-Rom
    /// tangents at the two surrounding nodes.
    /// </summary>
    private float thetaAt(int link, float t)
    {
        // Ease the angle's progress only (never the radius), so the sweep feel changes but timing does
        // not. Endpoints are preserved (ease(0) = 0, ease(1) = 1), so node angles/times stay exact.
        if (linkEasing[link] != Easing.None)
            t = (float)Interpolation.ApplyEasing(linkEasing[link], t);

        float theta0 = nodeRadians[link];
        float theta1 = nodeRadians[link + 1];

        // Linear by default — preserves the link's exact authored geometry. Only opted-in links smooth.
        if (!linkSmooth[link])
            return lerp(theta0, theta1, t);

        // Tangents are dθ/dtime; scale by the link duration to express them per unit of t.
        float h = (float)(nodeTimes[link + 1] - nodeTimes[link]);
        float m0 = nodeThetaSlopes[link] * h;
        float m1 = nodeThetaSlopes[link + 1] * h;

        float t2 = t * t;
        float t3 = t2 * t;

        // Cubic Hermite basis functions.
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;

        return h00 * theta0 + h10 * m0 + h01 * theta1 + h11 * m1;
    }

    private static float toRadians(float degrees) => degrees * MathF.PI / 180f;

    private static float lerp(float a, float b, float t) => a + (b - a) * t;

    private static bool approxEqual(Vector2 a, Vector2 b) => (a - b).LengthSquared < 0.0001f;

    /// <summary>
    /// Clips a link's parameter range [0, 1] to the sub-range whose linearly-interpolated radius lies
    /// within [0, <paramref name="ringRadius"/>], using Liang–Barsky. The lower crossing is the point
    /// currently emerging from the centre; the upper crossing is where the curve meets the ring.
    /// Returns false if no part of the link is currently visible.
    /// </summary>
    private static bool clipToBand(float rA, float rB, float ringRadius, out float tLo, out float tHi)
    {
        tLo = 0f;
        tHi = 1f;

        // radius(t) = rA + (rB - rA) * t; keep 0 <= radius(t) <= ringRadius.
        float d = rB - rA;

        return clipEdge(-d, rA, ref tLo, ref tHi) // radius(t) >= 0
               && clipEdge(d, ringRadius - rA, ref tLo, ref tHi); // radius(t) <= ringRadius
    }

    private static bool clipEdge(float p, float q, ref float tLo, ref float tHi)
    {
        if (p == 0)
            return q >= 0; // link runs parallel to this boundary: visible only if already inside it

        float r = q / p;

        if (p < 0)
        {
            if (r > tHi) return false;

            if (r > tLo) tLo = r;
        }
        else
        {
            if (r < tLo) return false;

            if (r < tHi) tHi = r;
        }

        return true;
    }

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
