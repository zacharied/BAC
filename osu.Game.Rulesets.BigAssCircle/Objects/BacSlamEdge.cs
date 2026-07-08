using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacSlamEdge : BacHitObject, IHasAngle
{
    public required int AngleDeg { get; init; }
    public HorizontalDirection Side;
    public RotationalDirection Direction = RotationalDirection.Clockwise;
}
