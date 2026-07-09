using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Rulesets.BigAssCircle.Edit.Blueprints.Components;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit.Blueprints;

/// <summary>
/// Base selection blueprint: positions itself each frame at the object's (angle → x, time → y) point on
/// the editor timeline, mirroring how the editor drawables themselves are laid out (origin at the
/// bottom-centre, since the timeline scrolls downward).
///
/// When the object is within reach of a ghost band, a twin outline is drawn at the wrapped position and
/// positional input there is accepted too (hit-testing is done by translating the query point back onto
/// the main copy), which is what makes the band clones selectable and draggable.
/// </summary>
internal abstract partial class BacSelectionBlueprint<T> : HitObjectSelectionBlueprint<T>
    where T : BacHitObject, IHasAngle
{
    [Resolved]
    private Playfield playfield { get; set; } = null!;

    protected ScrollingHitObjectContainer HitObjectContainer => ((BacEditorPlayfield)playfield).HitObjectContainer;

    private Drawable? twin;

    protected BacSelectionBlueprint(T hitObject)
        : base(hitObject)
    {
        RelativeSizeAxes = Axes.None;
        // Matches the drawables: single-press outlines straddle their time line; duration blueprints
        // override to BottomCentre so their height spans start → end exactly.
        Origin = Anchor.Centre;
    }

    protected override void Update()
    {
        base.Update();

        var container = HitObjectContainer;

        var screen = container.ScreenSpacePositionAtTime(HitObject.StartTime);
        float localX = ComputeXFraction() * container.DrawWidth;
        screen.X = container.ToScreenSpace(new Vector2(localX, 0)).X;

        Position = Parent!.ToLocalSpace(screen) - AnchorPosition;

        updateTwin();
    }

    private void updateTwin()
    {
        if (TwinXFraction() is float twinX)
        {
            if (twin == null)
                AddInternal(twin = CreateTwinVisual());

            // blueprint space shares the playfield's scale, so the offset in container-local pixels applies directly.
            twin.X = (twinX - ComputeXFraction()) * HitObjectContainer.DrawWidth;
            twin.Show();
        }
        else
            twin?.Hide();
    }

    /// <summary>The outline shown over the ghost twin. Sized to the blueprint by default.</summary>
    protected virtual Drawable CreateTwinVisual() => new EditSquarePiece { RelativeSizeAxes = Axes.Both };

    /// <summary>The x position (as a fraction of the full editor width). Defaults to the object's angle.</summary>
    protected virtual float ComputeXFraction() => EditorAngleMapping.ToX(HitObject.AngleDeg);

    /// <summary>Where the ghost twin sits (x-fraction of the full width), or null when no twin is visible.</summary>
    protected virtual float? TwinXFraction() => EditorAngleMapping.GhostTwinX(HitObject.AngleDeg);

    public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
    {
        if (base.ReceivePositionalInputAt(screenSpacePos))
            return true;

        // accept input on the ghost twin by translating the query back onto the main copy.
        if (TwinXFraction() is float twinX)
            return base.ReceivePositionalInputAt(screenSpacePos - twinScreenOffset(twinX));

        return false;
    }

    private Vector2 twinScreenOffset(float twinX)
    {
        var container = HitObjectContainer;
        float offsetLocal = (twinX - ComputeXFraction()) * container.DrawWidth;
        return container.ToScreenSpace(new Vector2(offsetLocal, 0)) - container.ToScreenSpace(Vector2.Zero);
    }
}
