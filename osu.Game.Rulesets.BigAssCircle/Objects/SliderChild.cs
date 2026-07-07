namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class SliderChild : BacHitObject
{
    public required SliderBody Parent;
    public BacPathControlPoint ControlPoint;

    public SliderChild(BacPathControlPoint controlPoint)
    {
        ControlPoint = controlPoint;
    }
}
