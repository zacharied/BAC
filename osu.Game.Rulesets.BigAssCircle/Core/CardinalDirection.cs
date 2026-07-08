using System;

namespace osu.Game.Rulesets.BigAssCircle.Core;

public enum CardinalDirection
{
    East = 0,
    North = 1,
    West = 2,
    South = 3
}

public static class CardinalDirectionExtensions
{
    public static float ToRadians(this CardinalDirection direction)
    {
        return (int)direction * MathF.PI / 2;
    }

    public static int ToDegrees(this CardinalDirection direction)
    {
        return (int)direction * 90;
    }

    public static CardinalDirection FromAngle(int angleDeg)
    {
        return (CardinalDirection)((angleDeg % 360) / 90);
    }
}
