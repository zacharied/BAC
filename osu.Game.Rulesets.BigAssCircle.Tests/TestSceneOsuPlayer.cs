// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Beatmaps;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BigAssCircle.Tests
{
    [TestFixture]
    public partial class TestSceneOsuPlayer : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BigAssCircleRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            return BacTestBeatmapGenerator.GenerateBeatmap();
        }
    }
}
