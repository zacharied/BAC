using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

internal partial class ShoulderNoteSelectionBlueprint : BacSelectionBlueprint<ShoulderNote>
{
    public ShoulderNoteSelectionBlueprint(ShoulderNote note)
        : base(note)
    {
        Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE);
        InternalChild = new EditSquarePiece { RelativeSizeAxes = Axes.Both };
    }

    protected override float ComputeXFraction() => BacEditorPlayfield.ShoulderXFraction(HitObject.Side);

    // the shoulder lane strips sit well inside the grid; no wrap-around twin.
    protected override float? TwinXFraction() => null;
}
