using osu.Framework.Bindables;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public class BacPath
{
    public required BindableList<BacPathControlPoint> ControlPoints { get; init; }
}
