using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>A centered slam on the editor timeline: the arrow sprite (natively up) rotated to point down.</summary>
internal partial class EditorDrawableSlamCentered : EditorDrawableBacHitObject<BacSlamCentered>
{
    public EditorDrawableSlamCentered(BacSlamCentered hitObject)
        : base(hitObject)
    {
        Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE);
    }

    protected override Drawable CreateVisual() => new EditorSpritePiece("arrow")
    {
        Anchor = Anchor.Centre,
        Origin = Anchor.Centre,
        Rotation = 180,
    };
}
