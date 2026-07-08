using osu.Game.Rulesets.BigAssCircle.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

internal partial class SlamCenteredPlacementBlueprint : InstantPlacementBlueprint<BacSlamCentered>
{
    public SlamCenteredPlacementBlueprint()
        : base(new BacSlamCentered { AngleDeg = 0 })
    {
    }
}
