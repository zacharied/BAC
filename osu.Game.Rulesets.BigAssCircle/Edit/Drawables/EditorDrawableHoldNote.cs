using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// A hold note on the editor timeline: the square head at its start time with a translucent body
/// stretching over the duration (the scrolling container sets the height for
/// <see cref="osu.Game.Rulesets.Objects.Types.IHasDuration"/> objects; downward scrolling grows it
/// upward from the bottom origin).
/// </summary>
internal partial class EditorDrawableHoldNote : EditorDrawableBacHitObject<HoldNote>
{
    private readonly Container nestedContainer;

    public EditorDrawableHoldNote(HoldNote hitObject)
        : base(hitObject)
    {
        Width = EditorDrawableCardinalNote.NOTE_SIZE;
        AddInternal(nestedContainer = new Container { RelativeSizeAxes = Axes.Both });
    }

    protected override Drawable CreateVisual() => new Container
    {
        RelativeSizeAxes = Axes.Both,
        Children = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Y,
                Width = 12,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Colour = Color4.White,
                Alpha = 0.35f,
            },
            new EditorSpritePiece("square")
            {
                RelativeSizeAxes = Axes.None,
                Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE),
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
            },
        },
    };

    protected override DrawableHitObject CreateNestedHitObject(HitObject hitObject) => new EditorDrawableNestedStub((BacHitObject)hitObject);

    protected override void AddNestedHitObject(DrawableHitObject hitObject) => nestedContainer.Add(hitObject);

    protected override void ClearNestedHitObjects() => nestedContainer.Clear(false);
}
