using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacButtonHitObject : BacHitObject
{
    public CardinalDirection Direction { get; init; } = CardinalDirection.East;

    protected override HitWindows CreateHitWindows()
    {
        return base.CreateHitWindows();
    }
}
