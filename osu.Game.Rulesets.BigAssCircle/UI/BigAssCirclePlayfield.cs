// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Layout;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle.UI
{
    [Cached]
    public partial class BigAssCirclePlayfield : Playfield
    {
        private readonly LayoutValue layoutCache = new LayoutValue(Invalidation.DrawSize);

        private readonly Drawable arc = new Arc(0, 2 * MathF.PI)
        {
            Resolution = 128,
            Colour = Colour4.White,
        };

        private readonly Drawable radialLines = new PlayfieldRadialLines();

        protected override HitObjectContainer CreateHitObjectContainer()
        {
            return new BigAssCircleScrollingHitObjectContainer();
        }

        public BigAssCirclePlayfield()
        {
            AddLayout(layoutCache);
            Padding = new MarginPadding(150);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal([
                radialLines,
                CreateHitObjectContainer(),
                arc
            ]);
        }

        protected override void Update()
        {
            if (layoutCache.IsValid)
                return;

            layoutCache.Validate();
        }
    }
}
