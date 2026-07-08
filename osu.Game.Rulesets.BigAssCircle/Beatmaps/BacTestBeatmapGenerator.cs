using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Beatmaps;

public class BacTestBeatmapGenerator
{
    public static Beatmap<BacHitObject> GenerateBeatmap()
    {
        return new Beatmap<BacHitObject>()
        {
            HitObjects =
            [
                new SliderBody()
                {
                    AngleDeg = 0,
                    StartTime = 2000,
                    Side = HorizontalDirection.Right,
                    Path = new BacPath()
                    {
                        ControlPoints = new BindableList<BacPathControlPoint>([
                            new BacPathControlPoint()
                            {
                                RotationOffset = 0, TimeOffset = 1000
                            },
                            new BacPathControlPoint()
                            {
                                RotationOffset = 90, TimeOffset = 2000, SweepEasing = Easing.None
                            },
                            new BacPathControlPoint()
                            {
                                RotationOffset = 90, TimeOffset = 4000
                            }
                        ])
                    }
                },
                new SliderBody()
                {
                    AngleDeg = 0,
                    StartTime = 5000,
                    Side = HorizontalDirection.Left,
                    Path = new BacPath()
                    {
                        ControlPoints = new BindableList<BacPathControlPoint>([
                            new BacPathControlPoint()
                            {
                                RotationOffset = 0, TimeOffset = 1000
                            },
                            new BacPathControlPoint()
                            {
                                RotationOffset = 90, TimeOffset = 2000, SweepEasing = Easing.None
                            },
                            new BacPathControlPoint()
                            {
                                RotationOffset = 90, TimeOffset = 4000
                            }
                        ])
                    }
                },
                new CardinalNote()
                {
                    StartTime = 2000,
                    AngleDeg = 90,
                },
                new CardinalNote()
                {
                    StartTime = 2150,
                    AngleDeg = 90,
                },
                new CardinalNote()
                {
                    StartTime = 4000,
                    AngleDeg = 90,
                },
                new ShoulderNote()
                {
                    StartTime = 3000,
                    Side = HorizontalDirection.Right,
                },
                new ShoulderNote()
                {
                    StartTime = 3500,
                    Side = HorizontalDirection.Left,
                },
                new HoldNote()
                {
                    StartTime = 6000,
                    Duration = 1000,
                    AngleDeg = 180,
                },
                new HoldNote()
                {
                    StartTime = 8000,
                    Duration = 2000,
                    AngleDeg = 270,
                },
                // A hold shorter than the head press window: the deferred tail should just inherit the head.
                new HoldNote()
                {
                    StartTime = 11000,
                    Duration = 80,
                    AngleDeg = 0,
                }
            ]
        };
    }
}
