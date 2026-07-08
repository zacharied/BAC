using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

[Cached]
public partial class BigAssCircleHitObjectComposer : ScrollingHitObjectComposer<BacHitObject>
{
    /// <summary>The x-axis snapping increment, in degrees of absolute angle.</summary>
    public readonly BindableInt AngleSnap = new BindableInt(45);

    private static readonly int[] angle_snap_options = { 5, 15, 45, 90 };

    private DrawableBigAssCircleEditorRuleset drawableRuleset = null!;

    [Resolved]
    private EditorScreenWithTimeline? screenWithTimeline { get; set; }

    public BigAssCircleHitObjectComposer(BigAssCircleRuleset ruleset)
        : base(ruleset)
    {
    }

    public new BacEditorPlayfield Playfield => drawableRuleset.Playfield;

    protected override DrawableRuleset<BacHitObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods) =>
        drawableRuleset = new DrawableBigAssCircleEditorRuleset((BigAssCircleRuleset)ruleset, beatmap, mods);

    protected override ComposeBlueprintContainer CreateBlueprintContainer() => new BacBlueprintContainer(this);

    protected override BeatSnapGrid CreateBeatSnapGrid() => new BacBeatSnapGrid();

    protected override IReadOnlyList<CompositionTool> CompositionTools => new CompositionTool[]
    {
        new CardinalNoteCompositionTool(),
        new HoldNoteCompositionTool(),
        new ShoulderNoteCompositionTool(),
        new SlamCenteredCompositionTool(),
        new SlamEdgeCompositionTool(),
        new SliderCompositionTool(),
    };

    [BackgroundDependencyLoader]
    private void load()
    {
        EditorRadioButtonCollection angleSnapButtons;

        LeftToolbox.Add(new EditorToolboxGroup("angle snap")
        {
            Child = angleSnapButtons = new EditorRadioButtonCollection
            {
                RelativeSizeAxes = Axes.X,
                Items = angle_snap_options.Select(v => new RadioButton($"{v}°", () => AngleSnap.Value = v)).ToList(),
            },
        });

        angleSnapButtons.Items[System.Array.IndexOf(angle_snap_options, AngleSnap.Value)].Select();
    }

    /// <summary>
    /// The BAC snap: time via the base scrolling snap (beat divisor), x via the angle grid — snapped in
    /// the unwrapped band domain so ghost-band cursors stay put visually, with the wrapped angle
    /// reported on the returned <see cref="BacSnapResult"/>.
    /// </summary>
    public SnapResult FindSnappedAngleTimeAndPosition(Vector2 screenSpacePosition)
    {
        var timeSnapped = FindSnappedPositionAndTime(screenSpacePosition);

        if (timeSnapped.Playfield is not BacEditorPlayfield playfield)
            return timeSnapped;

        // The base scrolling snap recentres x to the playfield middle (columns don't care about x, we
        // do) — so take the snapped y from it but the angle from the original cursor position.
        var local = playfield.ToLocalSpace(screenSpacePosition);
        (float xFrac, int angleDeg) = EditorAngleMapping.SnapX(local.X / playfield.DrawWidth, AngleSnap.Value);
        local.X = xFrac * playfield.DrawWidth;
        local.Y = playfield.ToLocalSpace(timeSnapped.ScreenSpacePosition).Y;

        return new BacSnapResult(playfield.ToScreenSpace(local), timeSnapped.Time, angleDeg, playfield);
    }

    protected override void Update()
    {
        base.Update();

        // Match the scroll speed to the timeline zoom, mania-style.
        if (screenWithTimeline?.TimelineArea.Timeline != null)
            drawableRuleset.TimelineTimeRange = EditorClock.TrackLength / screenWithTimeline.TimelineArea.Timeline.CurrentZoom.Value / 2;
    }
}
