using System;
using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

internal class CardinalNote : Note, IHasCardinalDirection
{
    public CardinalDirection Direction { get; init; } = CardinalDirection.East;

    public override BigAssCircleButtonInput ButtonInput => Direction switch
    {
        CardinalDirection.East => BigAssCircleButtonInput.ButtonE,
        CardinalDirection.North => BigAssCircleButtonInput.ButtonN,
        CardinalDirection.West => BigAssCircleButtonInput.ButtonW,
        CardinalDirection.South => BigAssCircleButtonInput.ButtonS,
        _ => throw new InvalidOperationException()
    };
}
