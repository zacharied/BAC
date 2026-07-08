using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacSlamCentered : BacHitObject, IHasMutableAngle
{
    public required int AngleDeg { get; set; }
    public HorizontalDirection Side { get; init; } = HorizontalDirection.Left;
}
