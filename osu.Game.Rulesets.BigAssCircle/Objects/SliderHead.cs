namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class SliderHead : BacHitObject, IHasAngle
{
    private readonly SliderBody parent;

    public SliderHead(SliderBody parent)
    {
        this.parent = parent;
    }

    public int AngleDeg => parent.AngleDeg;
}
