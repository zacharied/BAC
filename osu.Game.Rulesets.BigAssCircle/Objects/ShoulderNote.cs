using System;
using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

internal partial class ShoulderNote : Note
{
    public HorizontalDirection Side = HorizontalDirection.Left;

    public override BigAssCircleButtonInput ButtonInput => Side switch
    {
        HorizontalDirection.Left => BigAssCircleButtonInput.ButtonL,
        HorizontalDirection.Right => BigAssCircleButtonInput.ButtonR,
        _ => throw new InvalidOperationException()
    };
}
