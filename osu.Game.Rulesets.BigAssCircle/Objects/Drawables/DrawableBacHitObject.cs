using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

public partial class DrawableBacHitObject<T> : DrawableHitObject<BacHitObject>
    where T : BacHitObject
{
    public new T HitObject => (T)base.HitObject;

    public DrawableBacHitObject(T hitObject)
        : base(hitObject)
    {
    }
}
