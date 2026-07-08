using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// An edge slam on the editor timeline: the arrow sprite pointed sideways. On the unrolled axis, angle
/// increases (counter-clockwise) to the right, so a clockwise slam points left and an anticlockwise slam
/// points right. Rotation tracks <see cref="BacSlamEdge.Direction"/> live since it's editable.
/// </summary>
internal partial class EditorDrawableSlamEdge : EditorDrawableBacHitObject<BacSlamEdge>
{
    private readonly List<EditorSpritePiece> arrows = new List<EditorSpritePiece>();

    public EditorDrawableSlamEdge(BacSlamEdge hitObject)
        : base(hitObject)
    {
        Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE);
    }

    protected override Drawable CreateVisual()
    {
        var arrow = new EditorSpritePiece("arrow")
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
        };
        arrows.Add(arrow);
        return arrow;
    }

    protected override void Update()
    {
        base.Update();

        foreach (var arrow in arrows)
            arrow.Rotation = HitObject.Direction == RotationalDirection.Clockwise ? -90 : 90;
    }
}
