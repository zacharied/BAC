// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Audio;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

public abstract class BacHitObject : HitObject
{
    protected BacHitObject()
    {
        Samples =
        [
            new(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT)
        ];
    }

    public override Judgements.Judgement CreateJudgement() => new();
}
