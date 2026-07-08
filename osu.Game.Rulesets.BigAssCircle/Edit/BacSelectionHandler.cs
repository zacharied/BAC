using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public partial class BacSelectionHandler : EditorSelectionHandler
{
    [Resolved]
    private HitObjectComposer composer { get; set; } = null!;

    private readonly Bindable<TernaryState> selectionAnticlockwiseState = new Bindable<TernaryState>();

    [BackgroundDependencyLoader]
    private void load()
    {
        selectionAnticlockwiseState.ValueChanged += state =>
        {
            switch (state.NewValue)
            {
                case TernaryState.False:
                    setEdgeSlamDirection(RotationalDirection.Clockwise);
                    break;

                case TernaryState.True:
                    setEdgeSlamDirection(RotationalDirection.Anticlockwise);
                    break;
            }
        };
    }

    private void setEdgeSlamDirection(RotationalDirection direction)
    {
        if (SelectedItems.OfType<BacSlamEdge>().All(s => s.Direction == direction))
            return;

        EditorBeatmap.PerformOnSelection(h =>
        {
            if (h is BacSlamEdge slam)
                slam.Direction = direction;
        });
    }

    protected override IEnumerable<MenuItem> GetContextMenuItemsForSelection(IEnumerable<SelectionBlueprint<HitObject>> selection)
    {
        if (selection.All(s => s.Item is BacSlamEdge))
        {
            yield return new TernaryStateToggleMenuItem("Anticlockwise")
            {
                State = { BindTarget = selectionAnticlockwiseState },
            };
        }

        foreach (var item in base.GetContextMenuItemsForSelection(selection))
            yield return item;
    }

    protected override void UpdateTernaryStates()
    {
        base.UpdateTernaryStates();

        selectionAnticlockwiseState.Value = GetStateFromSelection(
            EditorBeatmap.SelectedHitObjects.OfType<BacSlamEdge>(),
            s => s.Direction == RotationalDirection.Anticlockwise);
    }

    public override bool HandleMovement(MoveSelectionEvent<HitObject> moveEvent)
    {
        var playfield = ((BigAssCircleHitObjectComposer)composer).Playfield;

        // Convert the (already snapped) horizontal screen delta to a whole-degree rotation. The axis
        // wraps, so unlike mania's column clamping every selected object can rotate freely.
        float localDeltaX = playfield.ToLocalSpace(moveEvent.Blueprint.ScreenSpaceSelectionPoint + moveEvent.ScreenSpaceDelta).X
                            - playfield.ToLocalSpace(moveEvent.Blueprint.ScreenSpaceSelectionPoint).X;
        int deltaDeg = (int)Math.Round(localDeltaX / playfield.DrawWidth * EditorAngleMapping.TOTAL_DEGREES);

        if (deltaDeg != 0)
        {
            EditorBeatmap.PerformOnSelection(h =>
            {
                if (h is IHasMutableAngle mutable)
                    mutable.AngleDeg = EditorAngleMapping.NormalizeDeg(mutable.AngleDeg + deltaDeg);
            });
        }

        // Return true regardless so a pure time move (no angle change) still applies.
        return true;
    }
}
