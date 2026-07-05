// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.BigAssCircle.Replays
{
    public class BigAssCircleAutoGenerator : AutoGenerator<BigAssCircleReplayFrame>
    {
        public new Beatmap<BacHitObject> Beatmap => (Beatmap<BacHitObject>)base.Beatmap;

        public BigAssCircleAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
        }

        protected override void GenerateFrames()
        {
            Frames.Add(new BigAssCircleReplayFrame());

            foreach (BacHitObject hitObject in Beatmap.HitObjects)
            {
                Frames.Add(new BigAssCircleReplayFrame
                {
                    Time = hitObject.StartTime
                    // todo: add required inputs and extra frames.
                });
            }
        }
    }
}
