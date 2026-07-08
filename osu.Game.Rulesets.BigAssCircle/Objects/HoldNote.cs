using System;
using System.Threading;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.BigAssCircle.Objects;

/// <summary>
/// A held cardinal note — a <see cref="CardinalNote"/> with a <see cref="Duration"/>. It is drawn as a
/// cardinal head sprite trailing a straight radial line (the hold), and is hit by pressing the head's
/// button in time and keeping it held until the tail reaches the ring.
///
/// Like a cardinal note, its painted position comes from its raw <see cref="AngleDeg"/> (<see cref="IHasAngle"/>)
/// while its derived <see cref="Direction"/> only selects the button / note-lock lane, so it rides the same
/// cardinal lane a <see cref="CardinalNote"/> at the same angle would.
/// </summary>
internal class HoldNote : Note, IHasCardinalDirection, IHasMutableAngle, IHasDuration
{
    public required int AngleDeg { get; set; }

    public double Duration { get; set; }

    public double EndTime => StartTime + Duration;

    public CardinalDirection Direction => CardinalDirectionExtensions.FromAngle(AngleDeg);

    public override BigAssCircleButtonInput ButtonInput => Direction switch
    {
        CardinalDirection.East => BigAssCircleButtonInput.ButtonE,
        CardinalDirection.North => BigAssCircleButtonInput.ButtonN,
        CardinalDirection.West => BigAssCircleButtonInput.ButtonW,
        CardinalDirection.South => BigAssCircleButtonInput.ButtonS,
        _ => throw new InvalidOperationException()
    };

    /// <summary>
    /// The head note, judged like a <see cref="CardinalNote"/> at the hold's <see cref="StartTime"/>. Its
    /// result is folded into the hold's final (tail) judgement, which is deferred until the head is judged.
    /// </summary>
    public HoldNoteHead Head { get; private set; } = null!;

    protected override void CreateNestedHitObjects(CancellationToken cancellationToken)
    {
        base.CreateNestedHitObjects(cancellationToken);

        AddNested(Head = new HoldNoteHead(this)
        {
            StartTime = StartTime,
        });
    }
}
