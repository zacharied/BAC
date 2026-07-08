using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public class HoldNoteCompositionTool : CompositionTool
{
    public HoldNoteCompositionTool()
        : base("Hold")
    {
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.EditorHoldNote };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new HoldNotePlacementBlueprint();
}

public class ShoulderNoteCompositionTool : CompositionTool
{
    public ShoulderNoteCompositionTool()
        : base("Shoulder")
    {
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Bookmark };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new ShoulderNotePlacementBlueprint();
}

public class SlamCenteredCompositionTool : CompositionTool
{
    public SlamCenteredCompositionTool()
        : base("Center Slam")
    {
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.AngleDoubleDown };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new SlamCenteredPlacementBlueprint();
}

public class SlamEdgeCompositionTool : CompositionTool
{
    public SlamEdgeCompositionTool()
        : base("Edge Slam")
    {
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.AngleDoubleRight };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new SlamEdgePlacementBlueprint();
}

public class SliderCompositionTool : CompositionTool
{
    public SliderCompositionTool()
        : base("Slider")
    {
        TooltipText = "Left click places the start and each node; right click commits (needs at least one node).";
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.WaveSquare };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new SliderPlacementBlueprint();
}
