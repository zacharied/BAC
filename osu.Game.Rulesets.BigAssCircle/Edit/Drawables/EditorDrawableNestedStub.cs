using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Drawables;

/// <summary>
/// Invisible editor representation for nested hit objects (hold heads, slider nodes). Nested objects
/// need a drawable in the tree (see the "nested hit objects" gotcha in CLAUDE.md) but the editor shows
/// nothing for them; they simply auto-judge as time passes.
/// </summary>
internal partial class EditorDrawableNestedStub : DrawableHitObject<BacHitObject>
{
    public EditorDrawableNestedStub(BacHitObject hitObject)
        : base(hitObject)
    {
    }

    protected override void CheckForResult(bool userTriggered, double timeOffset)
    {
        if (timeOffset >= 0)
            ApplyMaxResult();
    }

    protected override void UpdateHitStateTransforms(ArmedState state)
    {
    }
}
