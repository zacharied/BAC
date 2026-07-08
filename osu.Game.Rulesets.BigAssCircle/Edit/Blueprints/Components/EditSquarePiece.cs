using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;

/// <summary>The yellow outline box used by note blueprints (the BAC analogue of mania's EditNotePiece).</summary>
internal partial class EditSquarePiece : CompositeDrawable
{
    private readonly Container border;

    public EditSquarePiece()
    {
        InternalChild = border = new Container
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            BorderThickness = 3,
            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                AlwaysPresent = true,
            },
        };
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colours)
    {
        border.BorderColour = colours.YellowDark;
    }
}
