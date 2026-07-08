using System.Linq;
using System.Threading;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class SliderBody : BacHitObject, IHasDuration, IHasAngle
{
    /// <summary>
    /// The initial direction of the path, in degrees. Each child control point's
    /// <see cref="BacPathControlPoint.RotationOffset"/> is applied relative to this.
    /// </summary>
    public required int AngleDeg { get; init; }

    public required HorizontalDirection Side;

    public required BacPath Path { get; init; }

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

    /// <summary>
    /// The absolute time of the node immediately preceding <paramref name="child"/> along the path —
    /// the start of the segment that ends at the child. For the first control point this is the head
    /// node at <see cref="HitObject.StartTime"/>.
    /// </summary>
    public double GetSegmentStartTime(SliderChild child)
    {
        // Node 0 is the head at StartTime; control point i is node i+1 at StartTime + TimeOffset.
        // IndexOf is by reference (each control point instance is unique), matching DrawableSliderBody.
        int index = Path.ControlPoints.IndexOf(child.ControlPoint);
        return index <= 0 ? StartTime : StartTime + Path.ControlPoints[index - 1].TimeOffset;
    }

    protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
    {
        AddNested(new SliderHead(this)
        {
            StartTime = StartTime,
        });

        foreach (var controlPoint in Path.ControlPoints)
        {
            var childHitObject = new SliderChild(this, controlPoint)
            {
                StartTime = StartTime + controlPoint.TimeOffset,
            };
            AddNested(childHitObject);
        }
    }
}
