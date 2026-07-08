// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.BigAssCircle.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Beatmaps
{
    public class BigAssCircleBeatmapConverter : BeatmapConverter<BacHitObject>
    {
        private readonly Random random;

        public BigAssCircleBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
            random = new Random(beatmap.BeatmapInfo.GetDisplayTitle().GetHashCode());
        }

        public override bool CanConvert() => true;

        protected override IEnumerable<BacHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            yield return new CardinalNote
            {
                Samples = original.Samples,
                StartTime = original.StartTime,
                AngleDeg = ((CardinalDirection)random.Next(Enum.GetValues<CardinalDirection>().Length)).ToDegrees()
            };
        }
    }
}
