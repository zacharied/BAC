using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

internal class CardinalNote : BacHitObject
{
    public CardinalDirection Direction { get; init; } = CardinalDirection.East;
}
