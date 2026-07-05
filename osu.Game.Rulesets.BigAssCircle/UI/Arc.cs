using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.UI;

public sealed partial class Arc : Container
{
    private const int RESOLUTION = 32;

    public BindableFloat StartRadians { get; }
    public BindableFloat EndRadians { get; }
    public BindableFloat Thickness { get; }

    private readonly LayoutValue layoutCache = new LayoutValue(Invalidation.DrawSize);
    private readonly List<Box> segments = new List<Box>(RESOLUTION);

    public Arc(float startRadians = 0, float endRadians = 0, float thickness = 5)
    {
        AddLayout(layoutCache);

        RelativeSizeAxes = Axes.Both;
        Size = Vector2.One;

        StartRadians = new BindableFloat(startRadians);
        EndRadians = new BindableFloat(endRadians);
        Thickness = new BindableFloat(thickness);

        StartRadians.ValueChanged += _ => layoutCache.Invalidate();
        EndRadians.ValueChanged += _ => layoutCache.Invalidate();
        Thickness.ValueChanged += _ => layoutCache.Invalidate();
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        for (int i = 0; i < RESOLUTION; i++)
        {
            var segment = new Box
            {
                Origin = Anchor.CentreLeft,
            };

            segments.Add(segment);
            AddInternal(segment);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (layoutCache.IsValid)
            return;

        int visibleSegments = 0;

        foreach (var line in regenerateLines())
        {
            updateSegment(segments[visibleSegments++], line);
        }

        for (int i = visibleSegments; i < segments.Count; i++)
            segments[i].Alpha = 0;

        layoutCache.Validate();
    }

    private void updateSegment(Box box, Line line)
    {
        float length = line.Rho;

        box.Alpha = length > 0 && Thickness.Value > 0 ? 1 : 0;
        box.Position = line.StartPoint;
        box.Size = new Vector2(length, Thickness.Value);
        box.Rotation = line.Theta * 180 / MathF.PI;
    }

    private IEnumerable<Line> regenerateLines()
    {
        var centre = ChildSize / 2;
        float radius = (MathF.Min(ChildSize.X, ChildSize.Y) - Thickness.Value) / 2;

        if (radius <= 0 || Thickness.Value <= 0)
            yield break;

        float angleSpan = EndRadians.Value - StartRadians.Value;

        if (MathF.Abs(angleSpan) <= 0.0001f)
            yield break;

        float step = angleSpan / RESOLUTION;

        for (int i = 0; i < RESOLUTION; i++)
        {
            float start = StartRadians.Value + step * i;
            float end = start + step;

            yield return new Line(
                centre + positionAt(start, radius),
                centre + positionAt(end, radius));
        }
    }

    private Vector2 positionAt(float radians, float radius) => new Vector2(MathF.Sin(radians) * radius, MathF.Cos(radians) * radius);
}
