// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.BigAssCircle.Replays
{
    public class BigAssCircleReplayFrame : ReplayFrame
    {
        public List<BigAssCircleAction> Actions = new List<BigAssCircleAction>();

        public BigAssCircleReplayFrame(BigAssCircleAction? button = null)
        {
            if (button.HasValue)
                Actions.Add(button.Value);
        }

        public override bool IsEquivalentTo(ReplayFrame other)
            => other is BigAssCircleReplayFrame scrollingFrame && Time == scrollingFrame.Time && Actions.SequenceEqual(scrollingFrame.Actions);
    }
}
