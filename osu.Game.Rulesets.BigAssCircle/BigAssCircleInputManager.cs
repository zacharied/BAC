// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.BigAssCircle
{
    public partial class BigAssCircleInputManager : RulesetInputManager<BigAssCircleAction>
    {
        public BigAssCircleInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.Unique)
        {
        }

        protected override bool OnJoystickAxisMove(JoystickAxisMoveEvent e)
        {
            return base.OnJoystickAxisMove(e);
        }
    }

    public enum BigAssCircleAction
    {
        [Description("Button E")]
        ButtonE,

        [Description("Button N")]
        ButtonN,

        [Description("Button W")]
        ButtonW,

        [Description("Button S")]
        ButtonS,

        [Description("Button L")]
        ButtonL,

        [Description("Button R")]
        ButtonR,
    }
}
