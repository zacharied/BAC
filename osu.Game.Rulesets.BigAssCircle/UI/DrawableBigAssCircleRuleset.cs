// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.BigAssCircle.Objects.Drawables;
using osu.Game.Rulesets.BigAssCircle.Replays;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.UI
{
    [Cached]
    public partial class DrawableBigAssCircleRuleset : DrawableRuleset<BacHitObject>
    {
        public DrawableBigAssCircleRuleset(BigAssCircleRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        protected override Playfield CreatePlayfield() => new BigAssCirclePlayfield() { Size = new Vector2(1, 1) };

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new BigAssCircleFramedReplayInputHandler(replay);

        public override DrawableHitObject<BacHitObject> CreateDrawableRepresentation(BacHitObject h) => h switch
        {
            BacPathStartHitObject path => new DrawableBacPath(path),
            _ => new DrawableBacButtonHitObject(h),
        };

        protected override PassThroughInputManager CreateInputManager() => new BigAssCircleInputManager(Ruleset?.RulesetInfo);
    }
}
