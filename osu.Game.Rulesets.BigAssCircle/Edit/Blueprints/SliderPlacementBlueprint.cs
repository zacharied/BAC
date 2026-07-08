using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Multi-click slider placement: the first left click sets the body (start time + angle), each further
/// left click appends a control-point node at the snapped cursor (which must be later in time than the
/// previous node), and a right click commits — requiring at least one node, per the format's contract.
/// A rubber-band segment previews the next node at the cursor.
/// </summary>
internal partial class SliderPlacementBlueprint : BacPlacementBlueprint<SliderBody>
{
    private readonly SmoothPath previewPath;
    private readonly EditSquarePiece cursorPiece;

    private int cursorAngleDeg;
    private double cursorTime;

    protected override bool IsValidForPlacement => base.IsValidForPlacement && HitObject.Path.ControlPoints.Count > 0;

    public SliderPlacementBlueprint()
        : base(new SliderBody
        {
            AngleDeg = 0,
            Side = HorizontalDirection.Left,
            Path = new BacPath { ControlPoints = new BindableList<BacPathControlPoint>() },
        })
    {
        InternalChildren = new Drawable[]
        {
            previewPath = new SmoothPath { PathRadius = 3 },
            cursorPiece = new EditSquarePiece
            {
                Size = new Vector2(EditorDrawableCardinalNote.NOTE_SIZE),
                Origin = Anchor.BottomCentre,
            },
        };
    }

    [BackgroundDependencyLoader]
    private void load(OsuColour colours)
    {
        previewPath.Colour = colours.Yellow;
    }

    public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
    {
        var result = base.UpdateTimeAndPosition(screenSpacePosition, fallbackTime);

        if (result is BacSnapResult bac)
            cursorAngleDeg = bac.AngleDeg;
        if (result.Time is double time)
            cursorTime = time;

        cursorPiece.Position = ToLocalSpace(result.ScreenSpacePosition);

        return result;
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        switch (e.Button)
        {
            case MouseButton.Left:
                if (PlacementActive == PlacementState.Waiting)
                    BeginPlacement(true);
                else
                    tryAddNode();
                return true;

            case MouseButton.Right:
                if (PlacementActive == PlacementState.Active)
                    EndPlacement(HitObject.Path.ControlPoints.Count > 0);
                return true;
        }

        return false;
    }

    private void tryAddNode()
    {
        var controlPoints = HitObject.Path.ControlPoints;

        double timeOffset = cursorTime - HitObject.StartTime;
        var previous = controlPoints.Count > 0 ? controlPoints[^1] : null;
        double previousOffset = previous?.TimeOffset ?? 0;

        // control points must always advance in time along the path.
        if (timeOffset <= previousOffset)
            return;

        int previousRotation = previous?.RotationOffset ?? 0;
        int previousAbsolute = EditorAngleMapping.NormalizeDeg(HitObject.AngleDeg + previousRotation);

        controlPoints.Add(new BacPathControlPoint
        {
            TimeOffset = timeOffset,
            RotationOffset = previousRotation + EditorAngleMapping.MinimalDiff(previousAbsolute, cursorAngleDeg),
        });

        ApplyDefaultsToHitObject();
    }

    protected override void Update()
    {
        base.Update();

        if (Composer == null)
            return;

        var container = Composer.Playfield.HitObjectContainer;
        float pxPerDeg = DrawWidth / EditorAngleMapping.TOTAL_DEGREES;

        var vertices = new List<Vector2>();

        if (PlacementActive == PlacementState.Active)
        {
            float headX = EditorAngleMapping.ToX(HitObject.AngleDeg) * DrawWidth;

            vertices.Add(new Vector2(headX, ToLocalSpace(container.ScreenSpacePositionAtTime(HitObject.StartTime)).Y));

            foreach (var cp in HitObject.Path.ControlPoints)
            {
                vertices.Add(new Vector2(
                    headX + cp.RotationOffset * pxPerDeg,
                    ToLocalSpace(container.ScreenSpacePositionAtTime(HitObject.StartTime + cp.TimeOffset)).Y));
            }

            // rubber-band to the cursor when it would form a valid next node.
            if (cursorTime - HitObject.StartTime > (HitObject.Path.ControlPoints.Count > 0 ? HitObject.Path.ControlPoints[^1].TimeOffset : 0))
                vertices.Add(cursorPiece.Position);
        }

        previewPath.Vertices = vertices;
        previewPath.Position = -previewPath.PositionInBoundingBox(Vector2.Zero);
    }
}
