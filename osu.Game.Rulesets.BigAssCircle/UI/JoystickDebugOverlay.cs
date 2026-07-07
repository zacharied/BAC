using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Graphics;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.UI;

/// <summary>
/// TEMPORARY diagnostic. Displays (and logs) the raw <see cref="JoystickButton"/> and the
/// <see cref="InputKey"/> a key binding would use for the most recent gamepad press. Use it to discover
/// which InputKey each physical button emits on a specific controller, then delete this file and the
/// single line that adds it in <see cref="BigAssCirclePlayfield"/>.
/// </summary>
public sealed partial class JoystickDebugOverlay : CompositeDrawable
{
    private readonly SpriteText text;

    public JoystickDebugOverlay()
    {
        RelativeSizeAxes = Axes.Both;

        InternalChild = text = new SpriteText
        {
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Position = new Vector2(10),
            Text = "press a gamepad button…",
            Font = OsuFont.Default.With(size: 20),
            Colour = Colour4.Yellow,
        };
    }

    protected override bool OnJoystickPress(JoystickPressEvent e)
    {
        InputKey inputKey = KeyCombination.FromJoystickButton(e.Button);
        string message = $"Joystick: {e.Button}  ->  InputKey.{inputKey}";

        text.Text = message;
        Logger.Log(message, level: LogLevel.Important);

        return false; // observe only — let the press continue to the hit objects
    }
}
