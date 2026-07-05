using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

public partial class DrawableBacPathChild : DrawableHitObject<BacPathChildHitObject>
{
    public DrawableBacPathChild(BacPathChildHitObject hitObject)
        : base(hitObject)
    {
    }

    protected override void CheckForResult(bool userTriggered, double timeOffset)
    {
        if (timeOffset >= 0)
            // todo: implement judgement logic
            ApplyMaxResult();
    }
}
