using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.BigAssCircle.Edit;

public partial class BacBeatSnapGrid : BeatSnapGrid
{
    protected override IEnumerable<Container> GetTargetContainers(HitObjectComposer composer)
    {
        yield return ((BacEditorPlayfield)composer.Playfield).UnderlayElements;
    }
}
