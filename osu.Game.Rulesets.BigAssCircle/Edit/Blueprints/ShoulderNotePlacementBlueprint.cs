using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Shoulder note placement: x snaps to the nearer of the two shoulder lane strips (which picks the
/// note's <see cref="ShoulderNote.Side"/>); time from the usual beat snap. Instant place on click.
/// </summary>
internal partial class ShoulderNotePlacementBlueprint : HitObjectPlacementBlueprint
{
    protected new ShoulderNote HitObject => (ShoulderNote)base.HitObject;

    [Resolved]
    private BigAssCircleHitObjectComposer? composer { get; set; }

    private readonly EditSquarePiece piece;

    public ShoulderNotePlacementBlueprint()
        : base(new ShoulderNote { Side = HorizontalDirection.Left })
    {
        RelativeSizeAxes = Axes.Both;

        InternalChild = piece = new EditSquarePiece
        {
            Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE),
            Origin = Anchor.Centre,
            Colour = Color4.MediumPurple,
        };
    }

    public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
    {
        var result = composer?.FindSnappedAngleTimeAndPosition(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

        base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

        if (composer != null)
        {
            var playfield = composer.Playfield;

            if (PlacementActive == PlacementState.Waiting)
            {
                // pick the side whose lane strip is angularly closer to the cursor.
                float cursorGridDeg = playfield.ToLocalSpace(screenSpacePosition).X / playfield.DrawWidth * EditorAngleMapping.TOTAL_DEGREES - EditorAngleMapping.GHOST_DEGREES;
                cursorGridDeg = EditorAngleMapping.NormalizeDeg(cursorGridDeg);
                HitObject.Side = wrapDistance(cursorGridDeg, BacEditorPlayfield.LEFT_SHOULDER_GRID_DEG) <= wrapDistance(cursorGridDeg, BacEditorPlayfield.RIGHT_SHOULDER_GRID_DEG)
                    ? HorizontalDirection.Left
                    : HorizontalDirection.Right;
            }

            piece.Position = new Vector2(
                BacEditorPlayfield.ShoulderXFraction(HitObject.Side) * DrawWidth,
                ToLocalSpace(result.ScreenSpacePosition).Y);
        }

        return result;
    }

    private static float wrapDistance(float a, float b)
    {
        float d = Math.Abs(EditorAngleMapping.NormalizeDeg(a - b));
        return Math.Min(d, 360 - d);
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != MouseButton.Left)
            return false;

        BeginPlacement(true);
        EndPlacement(true);
        return true;
    }

    // only replace another shoulder note on the same side, not anything sharing the beat.
    public override bool ReplacesExistingObject(Rulesets.Objects.HitObject existing) =>
        base.ReplacesExistingObject(existing) && existing is ShoulderNote shoulder && shoulder.Side == HitObject.Side;
}
