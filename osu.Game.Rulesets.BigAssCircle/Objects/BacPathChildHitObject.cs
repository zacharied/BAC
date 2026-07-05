using System.Threading;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacPathChildHitObject : BacHitObject
{
    public BacPathControlPoint ControlPoint;

    public BacPathChildHitObject(BacPathControlPoint controlPoint)
    {
        ControlPoint = controlPoint;
    }

    protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
    {
        base.CreateNestedHitObjects(cancellationToken);
    }
}
