using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacSlamCentered : BacHitObject, IHasAngle
{
    public required int AngleDeg { get; init; }
    public HorizontalDirection Side { get; init; } = HorizontalDirection.Left;
}
