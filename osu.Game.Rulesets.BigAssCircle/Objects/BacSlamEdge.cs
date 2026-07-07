using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacSlamEdge : BacHitObject
{
    public HorizontalDirection Side;
    public RotationalDirection Direction = RotationalDirection.Clockwise;
    public int Angle = 0;
}
