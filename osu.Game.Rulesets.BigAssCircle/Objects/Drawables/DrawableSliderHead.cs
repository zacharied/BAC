namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

public partial class DrawableSliderHead : DrawableBacHitObject<SliderHead>
{
    public DrawableSliderHead(SliderHead hitObject)
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
