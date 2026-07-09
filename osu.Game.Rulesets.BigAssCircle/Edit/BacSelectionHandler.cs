using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public partial class BacSelectionHandler : EditorSelectionHandler
{
    [Resolved]
    private HitObjectComposer composer { get; set; } = null!;

    private readonly Bindable<TernaryState> selectionAnticlockwiseState = new Bindable<TernaryState>();

    // Replaces the framework SelectionBox for slider selections (see Update) — the AABB box spans a huge,
    // useless area since a slider can sweep well past 360°.
    private SliderCountChip countChip = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        // right-aligned so it sits just to the LEFT of the final node it anchors to.
        AddInternal(countChip = new SliderCountChip { Alpha = 0, Origin = Anchor.CentreRight });

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

    protected override void Update()
    {
        base.Update();

        var sliders = SelectedBlueprints.OfType<SliderSelectionBlueprint>().ToList();

        // Only when the whole selection is slider(s): the framework SelectionBox's AABB is meaningless for
        // a path that can exceed 360°, so hide it and show just the count chip 20px left of the final node.
        if (sliders.Count > 0 && sliders.Count == SelectedBlueprints.Count)
        {
            SelectionBox.Alpha = 0;

            countChip.Text = SelectedItems.Count.ToString();
            countChip.Position = ToLocalSpace(sliders[0].FinalNodeScreenPosition) - new Vector2(20, 0);
            countChip.Alpha = 1;
        }
        else
        {
            // leave SelectionBox visibility to the base's own logic for non-slider selections.
            countChip.Alpha = 0;
        }
    }

    /// <summary>The small numbered chip shown at the top of a slider selection in place of the SelectionBox.</summary>
    internal partial class SliderCountChip : CompositeDrawable
    {
        private readonly Box background;
        private readonly OsuSpriteText text;

        public string Text
        {
            set => text.Text = value;
        }

        public SliderCountChip()
        {
            AutoSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                background = new Box { RelativeSizeAxes = Axes.Both },
                text = new OsuSpriteText
                {
                    Padding = new MarginPadding(2),
                    Font = OsuFont.Default.With(size: 11),
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            background.Colour = colours.YellowDark;
            text.Colour = colours.Gray0;
        }
    }
}
