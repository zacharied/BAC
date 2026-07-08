using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacSlamEdge : BacHitObject, IHasMutableAngle
{
    public required int AngleDeg { get; set; }
    public HorizontalDirection Side;
    public RotationalDirection Direction = RotationalDirection.Clockwise;
}
