using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Base placement blueprint: every mouse move runs the composer's angle+time snap and, while still
/// waiting for the first click, writes the snapped (wrap-normalised) angle and time onto the pending
/// hit object.
/// </summary>
internal abstract partial class BacPlacementBlueprint<T> : HitObjectPlacementBlueprint
    where T : BacHitObject, IHasMutableAngle
{
    protected new T HitObject => (T)base.HitObject;

    [Resolved]
    protected BigAssCircleHitObjectComposer? Composer { get; private set; }

    protected BacPlacementBlueprint(T hitObject)
        : base(hitObject)
    {
        RelativeSizeAxes = Axes.Both;
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != MouseButton.Left)
            return false;

        BeginPlacement(true);
        return true;
    }

    public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
    {
        var result = Composer?.FindSnappedAngleTimeAndPosition(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

        base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

        if (PlacementActive == PlacementState.Waiting && result is BacSnapResult bac)
            HitObject.AngleDeg = bac.AngleDeg;

        return result;
    }

    // only replace an object occupying the same spot, not anything sharing the beat (mania scopes this
    // to the column; our equivalent is the angle).
    public override bool ReplacesExistingObject(Rulesets.Objects.HitObject existing) =>
        base.ReplacesExistingObject(existing) && existing is IHasAngle angled && EditorAngleMapping.NormalizeDeg(angled.AngleDeg) == EditorAngleMapping.NormalizeDeg(HitObject.AngleDeg);
}
