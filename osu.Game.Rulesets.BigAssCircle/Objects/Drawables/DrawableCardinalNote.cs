// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables
{
    internal partial class DrawableCardinalNote : DrawableNote<CardinalNote>
    {
        /// <summary>
        /// Whether this object can be hit, given a time value. If non-null, hits are ignored while the
        /// function returns false. Assigned by the owning <see cref="UI.Lane"/>'s hit policy (note lock).
        /// </summary>
        private readonly Sprite sprite;

        public DrawableCardinalNote(CardinalNote hitObject)
            : base(hitObject)
        {
            Size = new Vector2(80);
            sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            sprite.Texture = textures.Get("square");
            AddInternal(sprite);
        }

        protected override void PrepareForUse()
        {
            // Apply note spawn effect
            sprite.ScaleTo(0).ScaleTo(1, 125, Easing.In);
        }

        /// <summary>
        /// Forces this object to be missed, disregarding <see cref="CheckForResult"/>. Used by the lane's
        /// hit policy to note-lock earlier objects when a later one is hit.
        /// </summary>
        public void MissForcefully() => ApplyMinResult();

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            const double duration = 1000;

            switch (state)
            {
                case ArmedState.Hit:
                    sprite
                        .Spin(700, RotationDirection.Clockwise)
                        .FadeOut(350, Easing.OutQuint)
                        .ScaleTo(new Vector2(2), 350, Easing.OutQuint)
                        .OnComplete(_ => Expire());
                    break;

                case ArmedState.Miss:
                    sprite.FadeColour(Color4.Red, duration);
                    sprite.FadeOut(duration, Easing.InQuint).OnComplete(_ => Expire());
                    break;
            }
        }
    }
}
