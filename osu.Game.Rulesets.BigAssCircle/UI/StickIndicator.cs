using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.BigAssCircle.Core;
using osuTK;

namespace osu.Game.Rulesets.BigAssCircle.UI;

public partial class StickIndicator : Container
{
    /// <summary>
    /// The rotation (in radians, same polar convention as the rest of the playfield:
    /// <c>x = sin(θ)·r, y = cos(θ)·r</c>) that the stick is currently pointing towards.
    /// Only meaningful while <see cref="Activated"/> is true.
    /// </summary>
    public float Angle { get; private set; }

    /// <summary>
    /// Whether the stick is currently deflected beyond the deadzone.
    /// </summary>
    public bool Activated { get; private set; }

    /// <summary>
    /// Magnitude of stick deflection below which the indicator is hidden.
    /// </summary>
    public float Deadzone = 0.2f;

    /// <summary>
    /// Angular width of the drawn arc, in radians.
    /// </summary>
    public float ArcWidth = MathF.PI / 2.5f;

    /// <summary>
    /// Radius of the arc relative to the playfield ring (1 = on the ring, &gt;1 = outside it).
    /// </summary>
    public float RadiusScale = 1.06f;

    private readonly JoystickAxisSource xAxis, yAxis;
    private float xAxisLast, yAxisLast;
    private Vector2 joystickPosition => new Vector2(xAxisLast, yAxisLast);

    public HorizontalDirection Side { get; set; }

    private Arc arc = null!;

    public StickIndicator(JoystickAxisSource xAxis, JoystickAxisSource yAxis, HorizontalDirection side)
    {
        this.xAxis = xAxis;
        this.yAxis = yAxis;
        this.Side = side;

        RelativeSizeAxes = Axes.Both;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        AddInternal(arc = new Arc(thickness: 12)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            RelativeSizeAxes = Axes.Both,
            Size = new Vector2(RadiusScale),
            Colour = Side == HorizontalDirection.Left ? Colour4.Blue : Colour4.Red,
            Alpha = 1f,
        });
    }

    protected override bool OnJoystickAxisMove(JoystickAxisMoveEvent e)
    {
        if (e.Axis.Source == xAxis)
        {
            xAxisLast = e.Axis.Value;
            return true;
        }

        if (e.Axis.Source == yAxis)
        {
            yAxisLast = e.Axis.Value;
            return true;
        }

        return false;
    }

    protected override void Update()
    {
        base.Update();

        var position = joystickPosition;

        Activated = position.Length > Deadzone;

        if (Activated)
        {
            // Same polar convention as PositionAtTime: x = sin(θ)·r, y = cos(θ)·r.
            // The joystick reports +x right and +y down, matching the screen, so θ = atan2(x, y)
            // places the arc on the ring in the direction the stick is pushed.
            Angle = MathF.Atan2(position.X, position.Y);

            arc.StartRadians.Value = Angle - ArcWidth / 2;
            arc.EndRadians.Value = Angle + ArcWidth / 2;
        }

        arc.Alpha = Activated ? 1 : 0;
    }
}
