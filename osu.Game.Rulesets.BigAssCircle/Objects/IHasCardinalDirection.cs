using osu.Game.Rulesets.BigAssCircle.Core;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

/// <summary>
/// A hit object that is placed radially in one of the four <see cref="CardinalDirection"/>s. The scrolling
/// container reads this to position the drawable along the direction's angle, and the <see cref="UI.Ring"/>
/// routes the object to the matching <see cref="UI.Lane"/>. Implemented by both <see cref="CardinalNote"/>
/// and <see cref="ShoulderNote"/> (whose <see cref="HorizontalDirection"/> side maps onto West/East).
/// </summary>
internal interface IHasCardinalDirection
{
    CardinalDirection Direction { get; }
}
