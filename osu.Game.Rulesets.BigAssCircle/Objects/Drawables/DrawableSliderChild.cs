using osu.Framework.Allocation;
using osu.Game.Rulesets.BigAssCircle.Input;
using osu.Game.Rulesets.BigAssCircle.Objects.Judgement;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

internal partial class DrawableSliderChild : DrawableHitObject<SliderChild>
{
    [Resolved]
    private AnalogInputManager analogInput { get; set; }

    public DrawableSliderChild(SliderChild hitObject)
        : base(hitObject)
    {
    }

    protected override JudgementResult CreateResult(Judgements.Judgement judgement)
    {
        return new SliderJudgementResult(HitObject, judgement);
    }

    protected override void CheckForResult(bool userTriggered, double timeOffset)
    {
        if (timeOffset >= 0)
            // todo: implement judgement logic
            ApplyMaxResult();
    }

    protected override void Update()
    {
        if (Result is null)
            return;

//        analogInput.SliderCatchers[HitObject.Parent.Side].IsCatchingAt(HitObject.Parent.)
    }
}
