// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BigAssCircle.Tests
{
    [TestFixture]
    public partial class TestSceneOsuPlayer : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BigAssCircleRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            return new Beatmap<BacHitObject>()
            {
                HitObjects =
                [
                    new BacPathStartHitObject()
                    {
                        StartTime = 2000,
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
                    new BacButtonHitObject()
                    {
                        StartTime = 2000,
                        Direction = CardinalDirection.North,
                        Samples =
                        [
                            new HitSampleInfo(HitSampleInfo.HIT_NORMAL)
                        ]
                    },
                    new BacButtonHitObject()
                    {
                        StartTime = 4000,
                        Direction = CardinalDirection.West
                    }
                ]
            };
        }
    }
}
