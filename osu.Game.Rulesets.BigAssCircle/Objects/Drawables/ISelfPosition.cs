namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables;

/// <summary>
/// Marks a drawable that computes its own geometry each frame (e.g. paths) rather than being
/// point-positioned by <see cref="UI.BigAssCircleScrollingHitObjectContainer"/>'s alive-loop.
/// The container skips positioning any drawable implementing this, leaving it to self-manage.
/// </summary>
public interface ISelfPosition
{
}
