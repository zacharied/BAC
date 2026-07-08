using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

/// <summary>
/// A <see cref="SnapResult"/> that additionally carries the angle-snapped, wrap-normalised angle the
/// cursor position corresponds to. Produced by
/// <see cref="BigAssCircleHitObjectComposer.FindSnappedAngleTimeAndPosition"/>.
/// </summary>
public class BacSnapResult : SnapResult
{
    public readonly int AngleDeg;

    public BacSnapResult(Vector2 screenSpacePosition, double? time, int angleDeg, Playfield? playfield = null)
        : base(screenSpacePosition, time, playfield)
    {
        AngleDeg = angleDeg;
    }
}
