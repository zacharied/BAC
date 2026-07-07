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

    /// <summary>
    /// One action per physical button. Each cardinal direction has two: the "…1" action is driven by the
    /// d-pad and the "…2" action by the matching face button. See <see cref="BigAssCircleButtonInput"/> for
    /// the collapsed view where a direction is a single logical input.
    /// </summary>
    public enum BigAssCircleAction
    {
        [Description("Button East (D-Pad)")]
        ButtonE1,

        [Description("Button East (Face)")]
        ButtonE2,

        [Description("Button North (D-Pad)")]
        ButtonN1,

        [Description("Button North (Face)")]
        ButtonN2,

        [Description("Button West (D-Pad)")]
        ButtonW1,

        [Description("Button West (Face)")]
        ButtonW2,

        [Description("Button South (D-Pad)")]
        ButtonS1,

        [Description("Button South (Face)")]
        ButtonS2,

        [Description("Button Left")]
        ButtonL,

        [Description("Button Right")]
        ButtonR,
    }

    /// <summary>
    /// The logical button set from before the d-pad/face split, where a single value (e.g.
    /// <see cref="ButtonE"/>) represents both physical buttons for a direction. Not wired into input yet;
    /// reachable from an <see cref="BigAssCircleAction"/> via <see cref="BacActionExtensions.ToButtonInput"/>.
    /// </summary>
    public enum BigAssCircleButtonInput
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
                BigAssCircleAction.ButtonE1 or BigAssCircleAction.ButtonE2 => CardinalDirection.East,
                BigAssCircleAction.ButtonN1 or BigAssCircleAction.ButtonN2 => CardinalDirection.North,
                BigAssCircleAction.ButtonW1 or BigAssCircleAction.ButtonW2 => CardinalDirection.West,
                BigAssCircleAction.ButtonS1 or BigAssCircleAction.ButtonS2 => CardinalDirection.South,
                _ => null
            };
        }

        /// <summary>
        /// Collapses an <see cref="BigAssCircleAction"/> onto the logical <see cref="BigAssCircleButtonInput"/>
        /// it belongs to, folding a direction's d-pad ("…1") and face ("…2") actions together.
        /// </summary>
        public static BigAssCircleButtonInput ToButtonInput(this BigAssCircleAction action)
        {
            return action switch
            {
                BigAssCircleAction.ButtonE1 or BigAssCircleAction.ButtonE2 => BigAssCircleButtonInput.ButtonE,
                BigAssCircleAction.ButtonN1 or BigAssCircleAction.ButtonN2 => BigAssCircleButtonInput.ButtonN,
                BigAssCircleAction.ButtonW1 or BigAssCircleAction.ButtonW2 => BigAssCircleButtonInput.ButtonW,
                BigAssCircleAction.ButtonS1 or BigAssCircleAction.ButtonS2 => BigAssCircleButtonInput.ButtonS,
                BigAssCircleAction.ButtonL => BigAssCircleButtonInput.ButtonL,
                BigAssCircleAction.ButtonR => BigAssCircleButtonInput.ButtonR,
                _ => throw new System.ArgumentOutOfRangeException(nameof(action), action, null)
            };
        }
    }
}
