using System.Linq;
using System.Threading;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacPathStartHitObject : BacHitObject, IHasDuration
{
    public required HorizontalDirection Side;
    public BacPath Path;

    /// <summary>
    /// The initial direction of the path, in degrees. Each child control point's
    /// <see cref="BacPathControlPoint.RotationOffset"/> is applied relative to this.
    /// </summary>
    public int DirectionDeg { get; init; }

    /// <summary>
    /// The duration of the path, derived from the furthest-in-time control point.
    /// The setter is a no-op as the value is always computed from <see cref="Path"/>.
    /// </summary>
    public double Duration
    {
        get => Path == null || Path.ControlPoints.Count == 0 ? 0 : Path.ControlPoints.Max(c => c.TimeOffset);
        set { }
    }

    public double EndTime => StartTime + Duration;

    public BacPathStartHitObject()
    {
    }

    protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
    {
        foreach (var controlPoint in Path.ControlPoints)
        {
            var childHitObject = new BacPathChildHitObject(controlPoint)
            {
                StartTime = StartTime + controlPoint.TimeOffset,
            };
            AddNested(childHitObject);
        }
    }
}
