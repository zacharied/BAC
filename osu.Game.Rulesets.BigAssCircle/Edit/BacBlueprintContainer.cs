using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public partial class BacBlueprintContainer : ComposeBlueprintContainer
{
    public new BigAssCircleHitObjectComposer Composer => (BigAssCircleHitObjectComposer)base.Composer;

    public BacBlueprintContainer(BigAssCircleHitObjectComposer composer)
        : base(composer)
    {
    }

    public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(HitObject hitObject)
    {
        switch (hitObject)
        {
            case SliderBody slider:
                return new SliderSelectionBlueprint(slider);

            case HoldNote hold:
                return new HoldNoteSelectionBlueprint(hold);

            case CardinalNote note:
                return new OutlineSelectionBlueprint<CardinalNote>(note);

            case ShoulderNote shoulder:
                return new ShoulderNoteSelectionBlueprint(shoulder);

            case BacSlamCentered slam:
                return new OutlineSelectionBlueprint<BacSlamCentered>(slam);

            case BacSlamEdge slam:
                return new OutlineSelectionBlueprint<BacSlamEdge>(slam);
        }

        return base.CreateHitObjectBlueprintFor(hitObject);
    }

    protected override SelectionHandler<HitObject> CreateSelectionHandler() => new BacSelectionHandler();

    protected sealed override DragBox CreateDragBox() => new ScrollingDragBox(Composer.Playfield);

    protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
    {
        Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;

        // The final movement position, relative to movementBlueprintOriginalPosition.
        Vector2 movePosition = blueprints.First().originalSnapPositions.First() + distanceTravelled;

        // Retrieve a snapped position.
        var result = Composer.FindSnappedAngleTimeAndPosition(movePosition);

        var referenceBlueprint = blueprints.First().blueprint;
        bool moved = SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(referenceBlueprint, result.ScreenSpacePosition - referenceBlueprint.ScreenSpaceSelectionPoint));
        if (moved)
            ApplySnapResultTime(result, referenceBlueprint.Item.StartTime);
        return moved;
    }
}
