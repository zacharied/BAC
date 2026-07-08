using System;
using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

internal partial class ShoulderNote : Note, IHasCardinalDirection, IHasAngle
{
    public required HorizontalDirection Side { get; set; }

    public int AngleDeg => Side.ToAngleDeg();

    public override BigAssCircleButtonInput ButtonInput => Side switch
    {
        HorizontalDirection.Left => BigAssCircleButtonInput.ButtonL,
        HorizontalDirection.Right => BigAssCircleButtonInput.ButtonR,
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// A left shoulder travels in the West lane, a right shoulder in the East lane.
    /// </summary>
    public CardinalDirection Direction => Side == HorizontalDirection.Left
        ? CardinalDirection.West
        : CardinalDirection.East;
}
