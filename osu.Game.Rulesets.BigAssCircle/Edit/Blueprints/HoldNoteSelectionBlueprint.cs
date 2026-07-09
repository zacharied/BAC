using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Hold note selection: an outline over the whole duration with draggable head (bottom, retimes the
/// start) and tail (top, retimes the end) handles, following mania's HoldNoteSelectionBlueprint.
/// </summary>
internal partial class HoldNoteSelectionBlueprint : BacSelectionBlueprint<HoldNote>
{
    [Resolved]
    private IEditorChangeHandler? changeHandler { get; set; }

    [Resolved]
    private EditorBeatmap? editorBeatmap { get; set; }

    [Resolved]
    private BigAssCircleHitObjectComposer? composer { get; set; }

    private HoldNoteEndDragPiece head = null!;

    public HoldNoteSelectionBlueprint(HoldNote hold)
        : base(hold)
    {
        Width = EditorDrawableCardinalNote.NOTE_SIZE;
        Origin = Anchor.BottomCentre;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        InternalChildren = new Drawable[]
        {
            new EditSquarePiece { RelativeSizeAxes = Axes.Both },
            head = new HoldNoteEndDragPiece
            {
                RelativeSizeAxes = Axes.X,
                Height = EditorDrawableCardinalNote.NOTE_SIZE,
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.Centre,
                DragStarted = () => changeHandler?.BeginChange(),
                Dragging = pos =>
                {
                    double endTime = HitObject.EndTime;
                    double proposedStartTime = timeAt(pos);

                    if (proposedStartTime >= endTime)
                        return;

                    HitObject.StartTime = proposedStartTime;
                    HitObject.Duration = endTime - proposedStartTime;
                    editorBeatmap?.Update(HitObject);
                },
                DragEnded = () => changeHandler?.EndChange(),
            },
            new HoldNoteEndDragPiece
            {
                RelativeSizeAxes = Axes.X,
                Height = 10,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.Centre,
                DragStarted = () => changeHandler?.BeginChange(),
                Dragging = pos =>
                {
                    double proposedEndTime = timeAt(pos);

                    if (HitObject.StartTime >= proposedEndTime)
                        return;

                    HitObject.Duration = proposedEndTime - HitObject.StartTime;
                    editorBeatmap?.Update(HitObject);
                },
                DragEnded = () => changeHandler?.EndChange(),
            },
        };
    }

    private double timeAt(Vector2 screenSpacePosition) =>
        composer?.FindSnappedAngleTimeAndPosition(screenSpacePosition).Time ?? HitObjectContainer.TimeAtScreenSpacePosition(screenSpacePosition);

    protected override void Update()
    {
        base.Update();

        Height = HitObjectContainer.LengthAtTime(HitObject.StartTime, HitObject.EndTime);
    }

    public override Quad SelectionQuad => ScreenSpaceDrawQuad;

    public override Vector2 ScreenSpaceSelectionPoint => head.ScreenSpaceDrawQuad.Centre;
}
