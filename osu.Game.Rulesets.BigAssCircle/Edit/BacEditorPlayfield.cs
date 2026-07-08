using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

/// <summary>
/// The rectangular editor timeline playfield: y is time (a standard vertically scrolling container),
/// x is the circle unrolled per <see cref="EditorAngleMapping"/>. Hosts the angle grid, the two
/// shoulder-note lane strips at the quadrant boundaries, and the darkened ghost wrap-around bands.
/// </summary>
public partial class BacEditorPlayfield : ScrollingPlayfield
{
    /// <summary>Grid degrees of the Left-shoulder lane strip (the West–South quadrant boundary, 225° absolute).</summary>
    public const int LEFT_SHOULDER_GRID_DEG = 45;

    /// <summary>Grid degrees of the Right-shoulder lane strip (the East–North quadrant boundary, 45° absolute).</summary>
    public const int RIGHT_SHOULDER_GRID_DEG = 225;

    /// <summary>Visual width of a shoulder lane strip, in degrees.</summary>
    public const float SHOULDER_STRIP_DEGREES = 16;

    /// <summary>Target container for the beat snap grid's scrolling line containers.</summary>
    public Container UnderlayElements { get; } = new Container { RelativeSizeAxes = Axes.Both };

    /// <summary>The x-fraction (of the full editor width) of a side's shoulder lane strip.</summary>
    public static float ShoulderXFraction(HorizontalDirection side)
    {
        int gridDeg = side == HorizontalDirection.Left ? LEFT_SHOULDER_GRID_DEG : RIGHT_SHOULDER_GRID_DEG;
        return (EditorAngleMapping.GHOST_DEGREES + gridDeg) / (float)EditorAngleMapping.TOTAL_DEGREES;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        const float ghost_frac = (float)EditorAngleMapping.GHOST_DEGREES / EditorAngleMapping.TOTAL_DEGREES;

        InternalChildren = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.Black,
                Alpha = 0.3f,
            },
            UnderlayElements,
            new AngleGrid { RelativeSizeAxes = Axes.Both },
            shoulderStrip(LEFT_SHOULDER_GRID_DEG),
            shoulderStrip(RIGHT_SHOULDER_GRID_DEG),
            HitObjectContainer,
            // ghost band dimming, above the hit objects so their clones read as "faded copies".
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Width = ghost_frac,
                Colour = Color4.Black,
                Alpha = 0.5f,
            },
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Width = ghost_frac,
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Colour = Color4.Black,
                Alpha = 0.5f,
            },
        };
    }

    private static Drawable shoulderStrip(int gridDeg) => new Box
    {
        RelativeSizeAxes = Axes.Both,
        RelativePositionAxes = Axes.X,
        Width = SHOULDER_STRIP_DEGREES / EditorAngleMapping.TOTAL_DEGREES,
        X = (EditorAngleMapping.GHOST_DEGREES + gridDeg) / (float)EditorAngleMapping.TOTAL_DEGREES,
        Origin = Anchor.TopCentre,
        Colour = Color4.MediumPurple,
        Alpha = 0.12f,
    };

    /// <summary>
    /// Vertical angle demarcations: bright lines at the cardinal (quadrant) boundaries with letter
    /// labels, medium lines every 45°, and faint lines at the current angle-snap increment. Lines
    /// continue through the ghost bands.
    /// </summary>
    private partial class AngleGrid : CompositeDrawable
    {
        [Resolved]
        private BigAssCircleHitObjectComposer? composer { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (composer != null)
                composer.AngleSnap.BindValueChanged(_ => regenerate(), true);
            else
                regenerate();
        }

        private void regenerate()
        {
            ClearInternal();

            var lines = new List<Drawable>();
            int snap = composer?.AngleSnap.Value ?? 45;

            // walk the full band domain in grid degrees.
            for (int gridDeg = -EditorAngleMapping.GHOST_DEGREES; gridDeg <= 360 + EditorAngleMapping.GHOST_DEGREES; gridDeg += 1)
            {
                bool cardinal = gridDeg % 90 == 0;
                bool major = gridDeg % 45 == 0;
                // snap increments are multiples of the absolute angle; grid degrees are offset by the
                // origin, so convert before testing.
                bool snapLine = EditorAngleMapping.NormalizeDeg(gridDeg + EditorAngleMapping.ANGLE_ORIGIN) % snap == 0;

                if (!cardinal && !major && !snapLine)
                    continue;

                float x = (EditorAngleMapping.GHOST_DEGREES + gridDeg) / (float)EditorAngleMapping.TOTAL_DEGREES;

                lines.Add(new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    RelativePositionAxes = Axes.X,
                    X = x,
                    Origin = Anchor.TopCentre,
                    Width = cardinal ? 2 : 1,
                    Colour = Color4.White,
                    Alpha = cardinal ? 0.4f : major ? 0.2f : 0.08f,
                });

                if (cardinal)
                {
                    lines.Add(new OsuSpriteText
                    {
                        RelativePositionAxes = Axes.X,
                        X = x,
                        Y = 4,
                        Origin = Anchor.TopCentre,
                        Text = cardinalLabel(gridDeg),
                        Colour = colours.Yellow,
                        Font = OsuFont.Default.With(size: 16, weight: FontWeight.Bold),
                    });
                }
            }

            InternalChildren = lines.ToArray();
        }

        private static string cardinalLabel(int gridDeg)
        {
            int absolute = EditorAngleMapping.NormalizeDeg(gridDeg + EditorAngleMapping.ANGLE_ORIGIN);
            return CardinalDirectionExtensions.FromAngle(absolute).ToString()[..1];
        }
    }
}
