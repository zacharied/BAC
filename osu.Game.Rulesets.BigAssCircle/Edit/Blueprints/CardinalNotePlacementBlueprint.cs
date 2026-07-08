using osu.Game.Rulesets.BigAssCircle.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

internal partial class CardinalNotePlacementBlueprint : InstantPlacementBlueprint<CardinalNote>
{
    public CardinalNotePlacementBlueprint()
        : base(new CardinalNote { AngleDeg = 0 })
    {
    }
}
