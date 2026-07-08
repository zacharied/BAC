using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public class CardinalNoteCompositionTool : CompositionTool
{
    public CardinalNoteCompositionTool()
        : base("Note")
    {
    }

    public override Drawable CreateIcon() => new SpriteIcon { Icon = OsuIcon.EditorNote };

    public override HitObjectPlacementBlueprint CreatePlacementBlueprint() => new CardinalNotePlacementBlueprint();
}
