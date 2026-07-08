using System;

namespace osu.Game.Rulesets.BigAssCircle.Core;

public enum HorizontalDirection
{
    Left = -1,
    Right = 1
}

public static class HorizontalDirectionExtensions
{
    public static int ToAngleDeg(this HorizontalDirection horizontalDirection) => horizontalDirection switch
    {
        HorizontalDirection.Right => 0,
        HorizontalDirection.Left => 180,
        _ => throw new InvalidOperationException()
    };

    public static HorizontalDirection FromAngle(int angleDeg)
    {
        return (HorizontalDirection)((angleDeg % 360) / 90);
    }
}
