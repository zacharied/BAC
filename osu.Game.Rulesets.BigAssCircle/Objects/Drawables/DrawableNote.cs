using System;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

internal abstract partial class DrawableNote<T> : DrawableBacHitObject<T>, IKeyBindingHandler<BigAssCircleAction>, IHittableNote
    where T : Note
{
    public Func<DrawableHitObject, double, bool>? CheckHittable { get; set; }

    protected DrawableNote(T hitObject)
        : base(hitObject)
    {
    }

    /// <summary>
    /// Forces this object to be missed, disregarding <see cref="CheckForResult"/>. Used by the lane's
    /// hit policy to note-lock earlier objects when a later one is hit.
    /// </summary>
    public void MissForcefully() => ApplyMinResult();

    protected override void CheckForResult(bool userTriggered, double timeOffset)
    {
        if (!userTriggered)
        {
            if (!HitObject.HitWindows.CanBeHit(timeOffset))
                ApplyMinResult();
            return;
        }

        var result = HitObject.HitWindows.ResultFor(timeOffset);

        if (result == HitResult.None)
            return;

        ApplyResult(result);
    }

    public bool OnPressed(KeyBindingPressEvent<BigAssCircleAction> e)
    {
        if (e.Action.ToButtonInput() != HitObject.ButtonInput)
            return false;

        // Note lock: only the earliest un-judged object in this lane may be hit.
        if (CheckHittable?.Invoke(this, Time.Current) == false)
            return false;

        // Consume the press when it actually lands a hit so a single tap can't also hit the next
        // object in the lane. The keybeam still lights up because it observes the press first (it sits
        // in front of the hit objects in the lane's input queue). Mirrors DrawableNote.OnPressed.
        return UpdateResult(true);
    }

    public void OnReleased(KeyBindingReleaseEvent<BigAssCircleAction> e)
    {
    }
}
