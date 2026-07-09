using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Slider selection: an outline strip over the body's duration with a draggable circle handle per
/// control-point node (dragging retimes/re-angles that node, clamped between its neighbours). With the
/// slider selected, pressing <c>T</c> inserts a new node at the cursor, kept time-ordered.
/// </summary>
internal partial class SliderSelectionBlueprint : BacSelectionBlueprint<SliderBody>
{
    [Resolved]
    private IEditorChangeHandler? changeHandler { get; set; }

    [Resolved]
    private EditorBeatmap? editorBeatmap { get; set; }

    [Resolved]
    private BigAssCircleHitObjectComposer? composer { get; set; }

    private Container<NodeDragPiece> nodeHandles = null!;
    private EditSquarePiece head = null!;

    private InputManager inputManager = null!;

    public SliderSelectionBlueprint(SliderBody slider)
        : base(slider)
    {
        Width = EditorDrawableCardinalNote.NOTE_SIZE;
        Origin = Anchor.BottomCentre;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        InternalChildren = new Drawable[]
        {
            head = new EditSquarePiece
            {
                RelativeSizeAxes = Axes.X,
                Height = EditorDrawableCardinalNote.NOTE_SIZE,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.Centre,
            },
            nodeHandles = new Container<NodeDragPiece> { RelativeSizeAxes = Axes.Both },
        };
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        inputManager = GetContainingInputManager()!;
    }

    protected override void Update()
    {
        base.Update();

        Height = HitObjectContainer.LengthAtTime(HitObject.StartTime, HitObject.EndTime);

        var controlPoints = HitObject.Path.ControlPoints;

        while (nodeHandles.Count > controlPoints.Count)
            nodeHandles.Remove(nodeHandles[^1], true);

        while (nodeHandles.Count < controlPoints.Count)
        {
            int index = nodeHandles.Count;
            nodeHandles.Add(new NodeDragPiece
            {
                DragStarted = () => changeHandler?.BeginChange(),
                Dragging = pos => dragNode(index, pos),
                DragEnded = () => changeHandler?.EndChange(),
            });
        }

        double duration = HitObject.Duration;
        if (duration <= 0)
            return;

        float pxPerDeg = HitObjectContainer.DrawWidth / EditorAngleMapping.TOTAL_DEGREES;
        float bodyGridDeg = EditorAngleMapping.ToGridDegrees(HitObject.AngleDeg);

        for (int i = 0; i < controlPoints.Count; i++)
        {
            var cp = controlPoints[i];

            // one handle per node, at the node's WRAPPED (on-grid) position — the polyline may draw the
            // node again in a ghost band via a wrap copy, but only this handle is interactable.
            float nodeGridDeg = EditorAngleMapping.ToGridDegrees(HitObject.AngleDeg + cp.RotationOffset);

            nodeHandles[i].Position = new Vector2(
                DrawWidth / 2 + (nodeGridDeg - bodyGridDeg) * pxPerDeg,
                DrawHeight * (float)(1 - cp.TimeOffset / duration));
        }
    }

    private void dragNode(int index, Vector2 screenSpacePosition)
    {
        if (composer == null || editorBeatmap == null)
            return;

        var controlPoints = HitObject.Path.ControlPoints;
        if (index >= controlPoints.Count)
            return;

        var cp = controlPoints[index];
        var result = composer.FindSnappedAngleTimeAndPosition(screenSpacePosition);

        if (result.Time is double proposedTime)
        {
            double proposedOffset = proposedTime - HitObject.StartTime;
            double minOffset = index > 0 ? controlPoints[index - 1].TimeOffset : 0;
            double? maxOffset = index < controlPoints.Count - 1 ? controlPoints[index + 1].TimeOffset : null;

            if (proposedOffset > minOffset && (maxOffset == null || proposedOffset < maxOffset))
                cp.TimeOffset = proposedOffset;
        }

        if (result is BacSnapResult bac)
        {
            int currentAbsolute = EditorAngleMapping.NormalizeDeg(HitObject.AngleDeg + cp.RotationOffset);
            cp.RotationOffset += EditorAngleMapping.MinimalDiff(currentAbsolute, bac.AngleDeg);
        }

        editorBeatmap.Update(HitObject);
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        if (e.Key != Key.T || e.Repeat || !IsSelected)
            return base.OnKeyDown(e);

        insertNodeAtCursor();
        return true;
    }

    private void insertNodeAtCursor()
    {
        if (composer == null || editorBeatmap == null)
            return;

        var result = composer.FindSnappedAngleTimeAndPosition(inputManager.CurrentState.Mouse.Position);

        if (result.Time is not double time || result is not BacSnapResult bac)
            return;

        double timeOffset = time - HitObject.StartTime;

        // nodes must come strictly after the head.
        if (timeOffset <= 0)
            return;

        var controlPoints = HitObject.Path.ControlPoints;

        int insertIndex = 0;
        while (insertIndex < controlPoints.Count && controlPoints[insertIndex].TimeOffset < timeOffset)
            insertIndex++;

        // don't stack two nodes on the exact same time.
        if (insertIndex < controlPoints.Count && controlPoints[insertIndex].TimeOffset == timeOffset)
            return;

        int previousRotation = insertIndex > 0 ? controlPoints[insertIndex - 1].RotationOffset : 0;
        int previousAbsolute = EditorAngleMapping.NormalizeDeg(HitObject.AngleDeg + previousRotation);

        changeHandler?.BeginChange();

        controlPoints.Insert(insertIndex, new BacPathControlPoint
        {
            TimeOffset = timeOffset,
            RotationOffset = previousRotation + EditorAngleMapping.MinimalDiff(previousAbsolute, bac.AngleDeg),
        });

        editorBeatmap.Update(HitObject);
        changeHandler?.EndChange();
    }

    public override Quad SelectionQuad => ScreenSpaceDrawQuad;

    public override Vector2 ScreenSpaceSelectionPoint => head.ScreenSpaceDrawQuad.Centre;
}
