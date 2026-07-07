using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

public partial class DrawableBacHitObject<T> : DrawableHitObject<T>
    where T : BacHitObject
{
    public DrawableBacHitObject(T hitObject)
        : base(hitObject)
    {
    }
}
