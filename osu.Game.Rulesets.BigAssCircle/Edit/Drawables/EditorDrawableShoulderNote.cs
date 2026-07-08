using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// A shoulder note on the editor timeline: a purple square drawn in its side's dedicated lane strip at
/// the quadrant boundary (not at the note's actual in-game angle, which is fixed to West/East).
/// </summary>
internal partial class EditorDrawableShoulderNote : EditorDrawableBacHitObject<ShoulderNote>
{
    public EditorDrawableShoulderNote(ShoulderNote hitObject)
        : base(hitObject)
    {
        Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE);
    }

    protected override Drawable CreateVisual() => new EditorSpritePiece("square") { Colour = Color4.MediumPurple };

    protected override float ComputeXFraction() => BacEditorPlayfield.ShoulderXFraction(HitObject.Side);

    // the shoulder lane strips sit well inside the grid; no wrap-around twin.
    protected override float? TwinXFraction() => null;
}
