// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Objects.Drawables
{
    public partial class DrawableBacButtonHitObject : DrawableHitObject<BacButtonHitObject>, IKeyBindingHandler<BigAssCircleAction>
    {
        private readonly Drawable box;

        public DrawableBacButtonHitObject(BacButtonHitObject hitObject)
            : base(hitObject)
        {
            Size = new Vector2(40);
            Origin = Anchor.Centre;
            box = new Box()
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.White,
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(box);
        }

        protected override void PrepareForUse()
        {
            // Apply note spawn effect
            box.ScaleTo(0).ScaleTo(1, 100D, Easing.In);
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (timeOffset >= 0)
                // todo: implement judgement logic
                ApplyMaxResult();
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            const double duration = 1000;

            switch (state)
            {
                case ArmedState.Hit:
                    box
                        .Spin(100, RotationDirection.Clockwise)
                        .FadeOut(350, Easing.OutQuint)
                        .OnComplete(_ => Expire());
                    break;

                case ArmedState.Miss:
                    box.FadeColour(Color4.Red, duration);
                    box.FadeOut(duration, Easing.InQuint).OnComplete(_ => Expire());
                    break;
            }
        }

        public bool OnPressed(KeyBindingPressEvent<BigAssCircleAction> e)
        {
            if (e.Action.ToCardinalDirection() != HitObject.Direction)
                return false;

            return UpdateResult(true);
        }

        public void OnReleased(KeyBindingReleaseEvent<BigAssCircleAction> e)
        {
        }
    }
}
