using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

internal partial class EditorDrawableCardinalNote : EditorDrawableBacHitObject<CardinalNote>
{
    public const float NOTE_SIZE = 36;

    public EditorDrawableCardinalNote(CardinalNote hitObject)
        : base(hitObject)
    {
        Size = new Vector2(NOTE_SIZE);
    }

    protected override Drawable CreateVisual() => new EditorSpritePiece("square");
}
