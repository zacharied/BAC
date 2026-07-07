// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.BigAssCircle.Beatmaps;
using osu.Game.Rulesets.BigAssCircle.Mods;
using osu.Game.Rulesets.BigAssCircle.UI;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle
{
    public class BigAssCircleRuleset : Ruleset
    {
        public override string Description => "a very bigasscircle ruleset";

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod> mods = null) => new DrawableBigAssCircleRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) => new BigAssCircleBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) => new BigAssCircleDifficultyCalculator(RulesetInfo, beatmap);

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.Automation:
                    return new[] { new BigAssCircleModAutoplay() };

                default:
                    return Array.Empty<Mod>();
            }
        }

        public override string ShortName => "bigasscircle";

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => new[]
        {
            // Two gamepad buttons per direction, each now bound to its own action: the d-pad button drives
            // the "…1" action and the physically-matching face button drives the "…2" action.
            //
            // Each cardinal direction still sits at its matching on-screen position: an action is drawn at
            // (cosθ, -sinθ) where θ = direction.ToRadians(), so East = right, North = up, West = left,
            // South = down. Each physical button maps to the direction at that same screen position.
            //
            // The controller is opened as an SDL gamepad, so face buttons arrive as
            // X=Joystick1, A=Joystick2, B=Joystick3, Y=Joystick4 and the d-pad as JoystickHat1*.

            // Screen up -> North  (D-pad Up = N1, Y = N2)
            new KeyBinding(InputKey.JoystickHat1Up, BigAssCircleAction.ButtonN1),
            new KeyBinding(InputKey.Joystick4, BigAssCircleAction.ButtonN2),

            // Screen right -> East  (D-pad Right = E1, B = E2)
            new KeyBinding(InputKey.JoystickHat1Right, BigAssCircleAction.ButtonE1),
            new KeyBinding(InputKey.Joystick3, BigAssCircleAction.ButtonE2),

            // Screen down -> South  (D-pad Down = S1, A = S2)
            new KeyBinding(InputKey.JoystickHat1Down, BigAssCircleAction.ButtonS1),
            new KeyBinding(InputKey.Joystick2, BigAssCircleAction.ButtonS2),

            // Screen left -> West  (D-pad Left = W1, X = W2)
            new KeyBinding(InputKey.JoystickHat1Left, BigAssCircleAction.ButtonW1),
            new KeyBinding(InputKey.Joystick1, BigAssCircleAction.ButtonW2),
        };

        public override Drawable CreateIcon() => new SpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Text = ShortName[0].ToString(),
            Font = OsuFont.Default.With(size: 18),
        };

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}
