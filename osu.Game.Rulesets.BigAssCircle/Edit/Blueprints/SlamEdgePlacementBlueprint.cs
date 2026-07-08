using osu.Game.Rulesets.BigAssCircle.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

internal partial class SlamEdgePlacementBlueprint : InstantPlacementBlueprint<BacSlamEdge>
{
    public SlamEdgePlacementBlueprint()
        : base(new BacSlamEdge { AngleDeg = 0 })
    {
    }
}
