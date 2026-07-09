using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Mania-style hold placement: the first click commits the start time/angle, dragging stretches the
/// duration (in either direction — dragging upward swaps start/end), release commits.
/// </summary>
internal partial class HoldNotePlacementBlueprint : BacPlacementBlueprint<HoldNote>
{
    private Box bodyPiece = null!;
    private EditSquarePiece headPiece = null!;
    private EditSquarePiece tailPiece = null!;

    private double originalStartTime;

    protected override bool IsValidForPlacement => base.IsValidForPlacement && (PlacementActive == PlacementState.Waiting || Precision.DefinitelyBigger(HitObject.Duration, 0));

    public HoldNotePlacementBlueprint()
        : base(new HoldNote { AngleDeg = 0 })
    {
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colours)
    {
        InternalChildren = new Drawable[]
        {
            bodyPiece = new Box
            {
                Origin = Anchor.BottomCentre,
                Width = 12,
                Colour = colours.Yellow,
                Alpha = 0.4f,
            },
            headPiece = new EditSquarePiece
            {
                Origin = Anchor.Centre,
                Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE),
            },
            tailPiece = new EditSquarePiece
            {
                Origin = Anchor.Centre,
                Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE, 10),
            },
        };
    }

    protected override void Update()
    {
        base.Update();

        if (Composer == null)
            return;

        var container = Composer.Playfield.HitObjectContainer;
        float x = EditorAngleMapping.ToX(HitObject.AngleDeg) * DrawWidth;

        headPiece.Position = new Vector2(x, ToLocalSpace(container.ScreenSpacePositionAtTime(HitObject.StartTime)).Y);
        tailPiece.Position = new Vector2(x, ToLocalSpace(container.ScreenSpacePositionAtTime(HitObject.EndTime)).Y);

        // downward scrolling: the (later) tail sits above the head.
        float bottom = Math.Max(headPiece.Y, tailPiece.Y);
        float top = Math.Min(headPiece.Y, tailPiece.Y);

        bodyPiece.Position = new Vector2(x, bottom);
        bodyPiece.Height = bottom - top;
    }

    protected override void OnMouseUp(MouseUpEvent e)
    {
        if (e.Button != MouseButton.Left)
            return;

        base.OnMouseUp(e);
        EndPlacement(true);
    }

    public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
    {
        var result = base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);

        if (PlacementActive == PlacementState.Active)
        {
            if (result.Time is double endTime)
            {
                HitObject.StartTime = endTime < originalStartTime ? endTime : originalStartTime;
                HitObject.Duration = Math.Abs(endTime - originalStartTime);
            }
        }
        else
        {
            if (result.Time is double startTime)
                originalStartTime = startTime;
        }

        return result;
    }
}
