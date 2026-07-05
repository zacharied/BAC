// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle
{
    public partial class BigAssCircleInputManager : RulesetInputManager<BigAssCircleAction>
    {
        public BigAssCircleInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.All)
        {
        }

        protected override bool OnJoystickAxisMove(JoystickAxisMoveEvent e)
        {
            return base.OnJoystickAxisMove(e);
        }
    }

    public enum BigAssCircleAction
    {
        [Description("Button East")]
        ButtonE,

        [Description("Button North")]
        ButtonN,

        [Description("Button West")]
        ButtonW,

        [Description("Button South")]
        ButtonS,

        [Description("Button Left")]
        ButtonL,

        [Description("Button Right")]
        ButtonR,
    }

    public static class BacActionExtensions
    {
        public static CardinalDirection? ToCardinalDirection(this BigAssCircleAction action)
        {
            return action switch
            {
                BigAssCircleAction.ButtonE => CardinalDirection.East,
                BigAssCircleAction.ButtonN => CardinalDirection.North,
                BigAssCircleAction.ButtonW => CardinalDirection.West,
                BigAssCircleAction.ButtonS => CardinalDirection.South,
                _ => null
            };
        }
    }
}
