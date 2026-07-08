using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>Selection blueprint for any single-press object drawn as a note-sized sprite (cardinal notes, slams).</summary>
internal partial class OutlineSelectionBlueprint<T> : BacSelectionBlueprint<T>
    where T : BacHitObject, IHasAngle
{
    public OutlineSelectionBlueprint(T hitObject)
        : base(hitObject)
    {
        Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE);
        InternalChild = new EditSquarePiece { RelativeSizeAxes = Axes.Both };
    }
}
