// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle.UI;

/// <summary>
/// The circular arena — the analog of mania's <c>Stage</c>. It owns the shared furniture drawn in the
/// polar coordinate system (the radial lines and the outer <see cref="Arc"/> ring), hosts the four
/// <see cref="Lane"/>s, and routes each incoming button to the lane matching its
/// <see cref="CardinalDirection"/>.
///
/// Paths sweep across every direction, so they are not lane objects: they live in the ring's own
/// <see cref="HitObjectContainer"/> (as bar lines do in a mania stage). That container also provides the
/// polar geometry a <see cref="Objects.Drawables.DrawableBacPath"/> resolves each frame.
/// </summary>
[Cached]
public partial class Ring : Playfield
{
    private readonly Lane[] lanes = new Lane[Enum.GetValues<CardinalDirection>().Length];

    protected override HitObjectContainer CreateHitObjectContainer() => new BigAssCircleScrollingHitObjectContainer();

    public Ring()
    {
        RelativeSizeAxes = Axes.Both;

        var laneContainer = new Container { RelativeSizeAxes = Axes.Both };

        // Lanes are created here (not in load) so they exist before the first Add is routed to them.
        foreach (var direction in Enum.GetValues<CardinalDirection>())
        {
            var lane = new Lane(direction);
            lanes[(int)direction] = lane;

            laneContainer.Add(lane);
            AddNested(lane);
        }

        // Back-to-front: radial spokes, cross-lane paths, the lanes (keybeams + buttons on top), the ring.
        AddRangeInternal([
            new PlayfieldRadialLines(),
            HitObjectContainer,
            laneContainer,
            new Arc(0, 2 * MathF.PI)
            {
                Resolution = 128,
                Colour = Colour4.White,
            },
        ]);
    }

    public override void Add(HitObject hitObject)
    {
        if (hitObject is CardinalNote button)
            laneFor(button.Direction).Add(hitObject);
        else
            base.Add(hitObject);
    }

    public override bool Remove(HitObject hitObject)
    {
        if (hitObject is CardinalNote button)
            return laneFor(button.Direction).Remove(hitObject);

        return base.Remove(hitObject);
    }

    public override void Add(DrawableHitObject h)
    {
        if (h.HitObject is CardinalNote button)
            laneFor(button.Direction).Add(h);
        else
            base.Add(h);
    }

    public override bool Remove(DrawableHitObject h)
    {
        if (h.HitObject is CardinalNote button)
            return laneFor(button.Direction).Remove(h);

        return base.Remove(h);
    }

    private Lane laneFor(CardinalDirection direction) => lanes[(int)direction];
}
