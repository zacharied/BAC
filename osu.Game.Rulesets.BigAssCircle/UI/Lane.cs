// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects.Drawables;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle.UI;

/// <summary>
/// One <see cref="CardinalDirection"/>'s "column": the analog of mania's <c>Column</c>. Every button that
/// travels in this lane's direction lives in the lane's own <see cref="HitObjectContainer"/>, and the lane
/// runs its own <see cref="BacOrderedHitPolicy"/> over just those objects — so note lock is independent per
/// lane. The lane also owns the <see cref="PlayfieldKeybeam"/> that lights up for its direction.
///
/// All lanes are full-size and share the same polar centre, so they physically overlap; the "lane" is a
/// logical grouping by direction, not a spatial region like a mania column.
/// </summary>
[Cached]
public partial class Lane : Playfield
{
    public readonly CardinalDirection Direction;

    private readonly BacOrderedHitPolicy hitPolicy;

    protected override HitObjectContainer CreateHitObjectContainer() => new BigAssCircleScrollingHitObjectContainer();

    public Lane(CardinalDirection direction)
    {
        Direction = direction;
        RelativeSizeAxes = Axes.Both;

        // Scoped to this lane's container, so it only ever considers this direction's objects.
        hitPolicy = new BacOrderedHitPolicy(HitObjectContainer);
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        var keybeam = new PlayfieldKeybeam(Direction);

        // The keybeam draws behind the hit objects (a proxy renders it at the back), but the real drawable
        // sits in front of them in the input queue. That way it observes each press (returning false) before
        // a button consumes it, so it still lights up even though a hit now consumes the press. Mirrors the
        // way mania proxies its column pieces to separate draw order from input order.
        AddRangeInternal([
            keybeam.CreateProxy(),
            HitObjectContainer,
            keybeam,
        ]);
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        NewResult += onNewResult;
    }

    protected override void OnNewDrawableHitObject(DrawableHitObject drawableHitObject)
    {
        base.OnNewDrawableHitObject(drawableHitObject);

        if (drawableHitObject is DrawableBacButtonHitObject button)
            button.CheckHittable = hitPolicy.IsHittable;
    }

    private void onNewResult(DrawableHitObject judgedObject, JudgementResult result)
    {
        if (result.IsHit)
            hitPolicy.HandleHit(judgedObject);
    }

    protected override void Dispose(bool isDisposing)
    {
        // Must happen before children are disposed in the base call.
        NewResult -= onNewResult;
        base.Dispose(isDisposing);
    }
}
