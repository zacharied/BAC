namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class SliderChild : BacHitObject, IHasAngle
{
    public SliderBody Parent;
    public BacPathControlPoint ControlPoint;

    public SliderChild(SliderBody parent, BacPathControlPoint controlPoint)
    {
        Parent = parent;
        ControlPoint = controlPoint;
    }

    public int AngleDeg => Parent.AngleDeg + ControlPoint.RotationOffset;
}
