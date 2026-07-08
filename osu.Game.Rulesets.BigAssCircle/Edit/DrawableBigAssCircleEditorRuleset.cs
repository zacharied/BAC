using System.Collections.Generic;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.BigAssCircle.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

/// <summary>
/// The editor's drawable ruleset — entirely separate from gameplay's polar presentation. A standard
/// vertically scrolling (downward, mania-style: judgement at the bottom, future above) ruleset hosting
/// <see cref="BacEditorPlayfield"/>, with simplified editor drawables for every hit object type.
/// </summary>
public partial class DrawableBigAssCircleEditorRuleset : DrawableScrollingRuleset<BacHitObject>
{
    /// <summary>When set, the scroll speed tracks the editor timeline's zoom (see the composer's Update).</summary>
    public double? TimelineTimeRange { get; set; }

    public new BacEditorPlayfield Playfield => (BacEditorPlayfield)base.Playfield;

    public DrawableBigAssCircleEditorRuleset(BigAssCircleRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
        : base(ruleset, beatmap, mods)
    {
        Direction.Value = ScrollingDirection.Down;
        TimeRange.Value = 3000;
        VisualisationMethod = ScrollVisualisationMethod.Constant;
    }

    protected override bool UserScrollSpeedAdjustment => false;

    protected override Playfield CreatePlayfield() => new BacEditorPlayfield();

    protected override PassThroughInputManager CreateInputManager() => new BigAssCircleInputManager(Ruleset?.RulesetInfo);

    protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new BigAssCircleFramedReplayInputHandler(replay);

    public override DrawableHitObject<BacHitObject> CreateDrawableRepresentation(BacHitObject h) => h switch
    {
        SliderBody slider => new EditorDrawableSliderBody(slider),
        HoldNote hold => new EditorDrawableHoldNote(hold),
        CardinalNote note => new EditorDrawableCardinalNote(note),
        ShoulderNote shoulder => new EditorDrawableShoulderNote(shoulder),
        BacSlamCentered slam => new EditorDrawableSlamCentered(slam),
        BacSlamEdge slam => new EditorDrawableSlamEdge(slam),
        _ => null!,
    };

    protected override void Update()
    {
        if (TimelineTimeRange is double timelineRange)
            TimeRange.Value = timelineRange;

        base.Update();
    }
}
