using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Placement for single-press objects (cardinal notes, slams): an outline follows the snapped cursor
/// and a left click places immediately.
/// </summary>
internal abstract partial class InstantPlacementBlueprint<T> : BacPlacementBlueprint<T>
    where T : BacHitObject, IHasMutableAngle
{
    private readonly EditSquarePiece piece;

    protected InstantPlacementBlueprint(T hitObject)
        : base(hitObject)
    {
        InternalChild = piece = new EditSquarePiece
        {
            Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE),
            Origin = Anchor.Centre,
        };
    }

    public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
    {
        var result = base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);
        piece.Position = ToLocalSpace(result.ScreenSpacePosition);
        return result;
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != MouseButton.Left)
            return false;

        base.OnMouseDown(e);
        EndPlacement(true);
        return true;
    }
}
