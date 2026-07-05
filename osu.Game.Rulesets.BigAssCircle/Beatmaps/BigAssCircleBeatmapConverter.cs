// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Objects.Types;

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

        // todo: Check for conversion types that should be supported (ie. Beatmap.HitObjects.Any(h => h is IHasXPosition))
        // https://github.com/ppy/osu/tree/master/osu.Game/Rulesets/Objects/Types
        public override bool CanConvert() => true;

        protected override IEnumerable<BacHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            if (original is IHasPath path)
            {
                var bacPath = new BacPath();

                foreach (var child in path.Path.ControlPoints)
                {
                    bacPath.ControlPoints.Add(new BacPathControlPoint
                    {
                        RotationOffset = random.Next(360),
                        TimeOffset = path.Duration
                    });
                }

                yield return new BacPathStartHitObject() { StartTime = original.StartTime, Path = bacPath };
            }
            else
            {
                yield return new BacButtonHitObject
                {
                    Samples = original.Samples,
                    StartTime = original.StartTime,
                    Direction = (CardinalDirection)random.Next(Enum.GetValues<CardinalDirection>().Length)
                };
            }
        }
    }
}
