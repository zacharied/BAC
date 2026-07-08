using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// A slider on the editor timeline. The drawable itself is a note-width strip spanning the duration
/// (positioned at the body's angle); the <see cref="SliderPolylineVisual"/> draws the actual node
/// polyline, which freely extends horizontally beyond the strip.
/// </summary>
internal partial class EditorDrawableSliderBody : EditorDrawableBacHitObject<SliderBody>
{
    private readonly Container nestedContainer;

    public EditorDrawableSliderBody(SliderBody hitObject)
        : base(hitObject)
    {
        Width = EditorDrawableCardinalNote.NOTE_SIZE;
        AddInternal(nestedContainer = new Container { RelativeSizeAxes = Axes.Both });
    }

    protected override Drawable CreateVisual() => new SliderPolylineVisual(HitObject);

    protected override DrawableHitObject CreateNestedHitObject(HitObject hitObject) => new EditorDrawableNestedStub((BacHitObject)hitObject);

    protected override void AddNestedHitObject(DrawableHitObject hitObject) => nestedContainer.Add(hitObject);

    protected override void ClearNestedHitObjects() => nestedContainer.Clear(false);
}
