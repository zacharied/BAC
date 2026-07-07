// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Game.Rulesets.BigAssCircle.Objects.Drawables;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle.UI;

/// <summary>
/// Ensures that only the most recent <see cref="Objects.BacHitObject"/> in a single <see cref="Lane"/> is
/// hittable — the "note lock". Mirrors mania's <c>OrderedHitPolicy</c>, but because each <see cref="Lane"/>
/// owns its own <see cref="HitObjectContainer"/>, the policy only ever sees that lane's objects. Note lock
/// is therefore lane-independent: hitting (or missing) a north note never locks out an east note.
/// </summary>
public class BacOrderedHitPolicy
{
    private readonly HitObjectContainer hitObjectContainer;

    public BacOrderedHitPolicy(HitObjectContainer hitObjectContainer)
    {
        this.hitObjectContainer = hitObjectContainer;
    }

    /// <summary>
    /// Determines whether a <see cref="DrawableHitObject"/> can be hit at a point in time. Only the most
    /// recent object in the lane can be hit; an earlier object's window cannot extend past the next one.
    /// </summary>
    public bool IsHittable(DrawableHitObject hitObject, double time)
    {
        var nextObject = hitObjectContainer.AliveObjects.GetNext(hitObject);
        return nextObject == null || time < nextObject.HitObject.StartTime;
    }

    /// <summary>
    /// Handles an object being hit, force-missing every earlier un-judged object in the same lane so a
    /// skipped note cannot be hit after a later one.
    /// </summary>
    public void HandleHit(DrawableHitObject hitObject)
    {
        foreach (var obj in enumerateHitObjectsUpTo(hitObject.HitObject.StartTime))
        {
            if (obj.Judged)
                continue;

            if (obj is DrawableBacButtonHitObject button)
                button.MissForcefully();
        }
    }

    private IEnumerable<DrawableHitObject> enumerateHitObjectsUpTo(double targetTime)
    {
        foreach (var obj in hitObjectContainer.AliveObjects)
        {
            if (obj.HitObject.GetEndTime() >= targetTime)
                yield break;

            yield return obj;
        }
    }
}
