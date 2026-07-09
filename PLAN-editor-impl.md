# Editor implementation plan

Implements the spec in `PLAN-editor.md`: a mania-style compose screen where the y-axis is time and the
x-axis is the circle unrolled. Modeled directly on `osu.Game.Rulesets.Mania/Edit/*` (in
`LocalDependencies/osu`), reusing the shared editor framework (`osu.Game/Rulesets/Edit`,
`osu.Game/Screens/Edit/Compose/Components`).

**Decisions locked in:**

- **Ghost bands are wrap-aware from v1.** One blueprint per object draws + hit-tests a twin copy offset
  by ±360° when the object is within 30° of an edge; clicks inside a band wrap the angle in the snap
  function. Playfield drawables draw a darkened visual twin the same way.
- **Shoulder lanes are strips at the quadrant boundaries:** Left at 225° (West–South boundary,
  x = 12.5%), Right at 45° (East–North boundary, x = 62.5%).
- **Slider input:** multi-click placement (click = body start, further clicks = nodes, right-click/Enter
  commits, ≥1 node required) plus a hotkey (`T`) that inserts a node at the cursor on a selected slider.
- **Persistence is deferred.** V1 target is a fully working compose screen inside an `EditorTestScene`.
  Legacy-.osu encode/decode roundtrip is a follow-up milestone.

## Coordinate mapping (the one new idea; everything else is the mania pattern)

- x-axis ↔ angle: left edge = West (180°), increasing CCW (in the game's polar convention) to the right.
  `xFrac(angle) = normalizeDeg(angle − 180) / 360`, so South = 25%, East = 50%, North = 75%.
- Ghost bands: 30° extra on each side. The full playfield drawable is 420° wide; the main 360° grid is
  inset by `30/420` per side. A screen x inside a band maps to `angle ± 360` → normalized to `[0, 360)`.
  Centralise all of this in one static helper, e.g. `Edit/EditorAngleMapping.cs`
  (`AngleToX`, `XToAngle` (wrapping), `GhostTwinX(angle)` returning the ±360° twin x if within 30° of an
  edge, else null). Every drawable/blueprint/grid uses this helper — no local angle math.
- y-axis ↔ time: entirely handled by the stock `ScrollingPlayfield` / `ScrollingHitObjectContainer`
  (`TimeAtScreenSpacePosition`, `ScreenSpacePositionAtTime`, `LengthAtTime`) — this is what
  `ScrollingHitObjectComposer.FindSnappedPositionAndTime` (osu.Game/Rulesets/Edit/ScrollingHitObjectComposer.cs:120)
  requires, and why the editor gets time snapping for free.

## Model changes (prerequisite)

Editing mutates objects in place (`EditorBeatmap.Update(h)` after mutation), so init-only members need
setters:

- `IHasAngle.AngleDeg` → `{ get; set; }` on mutable implementers: `CardinalNote`, `HoldNote`,
  `SliderBody`, `BacSlamEdge`, `BacSlamCentered` (change `required … init` → `required … get; set;`).
  `ShoulderNote` keeps its derived angle; make `Side` settable instead. Derived `Direction` needs no change.
- `SliderBody.Path.ControlPoints` is already a `BindableList` — mutable. After editing it, call
  `EditorBeatmap.Update(slider)`; ApplyDefaults re-runs `CreateNestedHitObjects`, regenerating
  `SliderChild`ren. Verify `Duration` recomputes (it derives from the furthest control point).

## New code — all under `osu.Game.Rulesets.BigAssCircle/Edit/`

### Entry point

- `BigAssCircleRuleset.CreateHitObjectComposer()` → `new BigAssCircleHitObjectComposer(this)`.

### Composer

`BigAssCircleHitObjectComposer : ScrollingHitObjectComposer<BacHitObject>` (mania template:
`ManiaHitObjectComposer.cs`):

- `CreateDrawableRuleset` → `DrawableBigAssCircleEditorRuleset` (below).
- `CreateBlueprintContainer` → `BacBlueprintContainer`.
- `CreateBeatSnapGrid` → `BacBeatSnapGrid : BeatSnapGrid` targeting the editor playfield's underlay
  container (mania template: `ManiaBeatSnapGrid.cs`).
- `PlayfieldAtScreenSpacePosition` → the single editor playfield (whole area incl. ghost bands).
- `CompositionTools` → `[Cardinal, Hold, Shoulder, CenterSlam, EdgeSlam, Slider]` — each a trivial
  `CompositionTool` returning its placement blueprint (mania template: `NoteCompositionTool.cs`).
- **Angle snapping** (the part the base class doesn't do): `Bindable<int> AngleSnap` (default 45), exposed
  in a `LeftToolbox` `EditorToolboxGroup` (radio options: 1 / 5 / 15 / 45 / 90 — pattern at
  ScrollingHitObjectComposer.cs:49 for adding toolbox groups). Add
  `SnapResult FindSnappedAngleTimeAndPosition(Vector2 screenSpace)`: call the base
  `FindSnappedPositionAndTime` for time (it's non-virtual, which is fine — the beat snap grid only needs
  time), then `XToAngle` (wraps ghost bands), round to `AngleSnap`, convert back to snapped screen x.
  All BAC blueprints call this instead of the base method.

### Editor drawable ruleset + playfield

- `DrawableBigAssCircleEditorRuleset : DrawableScrollingRuleset<BacHitObject>` — vertical,
  `ScrollingDirection.Down` (mania orientation: judgement line at bottom, future above), fixed
  `TimeRange` (~3000 ms to start). Reuse `BigAssCircleFramedReplayInputHandler` for the autoplay mod the
  composer injects. `CreateDrawableRepresentation` maps **every** object type (incl. slams) to the editor
  drawables below. The gameplay `DrawableBigAssCircleRuleset`/polar playfield is untouched.
- `BacEditorPlayfield : ScrollingPlayfield` — rectangular, contains in z-order: underlay container (beat
  snap grid target), angle grid lines, shoulder strips, the `HitObjectContainer`, ghost-band dimming
  overlays. Grid lines: bright at 90° quadrant boundaries (W/S/E/N), medium at 45°, faint at the current
  `AngleSnap` increment (regenerate on snap change); lines continue into the bands.

### Editor drawables (`Edit/Drawables/`)

Simple sprite representations, separate from gameplay drawables. Shared base
`EditorDrawableBacHitObject<T>`: sets `X` (relative) from `AngleDeg` via the mapping helper every
`Update` (so drags reflect immediately), y comes from the scrolling container; owns the optional darkened
ghost twin sprite at `GhostTwinX`. Override `UpdateHitStateTransforms` to no-op so objects never
fade/expire while editing (mania does the equivalent in `EditorColumn.cs`).

- Cardinal → `square` texture. Shoulder → purple-tinted `square`. CenterSlam → `arrow` rotated to point
  down. EdgeSlam → `arrow` rotated left/right by `RotationalDirection`.
- Hold → square head + body rectangle with `Height = LengthAtTime(StartTime, EndTime)`.
- Slider → polyline (`SmoothPath`) over the nodes: node i at
  `(x = AngleToX(body.AngleDeg + cumulative RotationOffset, unwrapped), y = position at StartTime + TimeOffset)`,
  drawn on top of other objects (add slider drawables at a shallower depth, or proxy to a top layer).
  Nodes crossing an edge draw unwrapped into the bands in v1.

### Blueprints (`Edit/Blueprints/`)

Placement — base `BacPlacementBlueprint<T> : HitObjectPlacementBlueprint` (mania template:
`ManiaPlacementBlueprint.cs`): `UpdateTimeAndPosition` calls the composer's
`FindSnappedAngleTimeAndPosition`, writes `StartTime` (while `PlacementState.Waiting`) and the snapped,
wrapped `AngleDeg`, positions the preview piece at the snapped screen position.

- Cardinal / CenterSlam / EdgeSlam: instant place on left-click (`NotePlacementBlueprint.cs` pattern —
  `OnMouseDown` → `EndPlacement(true)`).
- Shoulder: x snaps to the nearer of the two strips; that strip sets `Side`; time from y; instant place.
- Hold: click sets start, drag sets duration (swap start/end when dragging upward), release commits —
  direct port of `HoldNotePlacementBlueprint.cs`.
- Slider: first click = body (`StartTime`, `AngleDeg`, empty `BacPath`); each further click appends
  `BacPathControlPoint { TimeOffset, RotationOffset }` relative to the body (reject clicks not later in
  time than the previous node; choose the RotationOffset nearest the previous node's angle so the line
  doesn't spin the long way); right-click/Enter commits when ≥1 node exists, else cancels.

Selection — base `BacSelectionBlueprint<T> : HitObjectSelectionBlueprint<T>` (mania template:
`ManiaSelectionBlueprint.cs`): each `Update`, position from
`HitObjectContainer.ScreenSpacePositionAtTime(StartTime)` (y) + `AngleToX` (x); contains the main outline
piece plus a ghost twin piece at `GhostTwinX`; override `ReceivePositionalInputAt` to hit-test both.

- Cardinal / Shoulder / slams: outline box only.
- Hold: head/tail drag handles that retime start/end with snap — direct port of
  `HoldNoteSelectionBlueprint.cs` (uses `IEditorChangeHandler.BeginChange/EndChange` around drags).
- Slider: circles at each node. Node drag → update that control point's `RotationOffset`/`TimeOffset`
  (clamped between neighbours' times); body-head drag → move whole object; `T` with cursor over the
  timeline → insert node at cursor time/angle, kept time-sorted. After any path mutation:
  `EditorBeatmap.Update(HitObject)`.

Container/handler:

- `BacBlueprintContainer : ComposeBlueprintContainer` — `CreateHitObjectBlueprintFor` switch,
  `CreateDragBox` → `ScrollingDragBox`, `TryMoveBlueprints` ported from `ManiaBlueprintContainer.cs`
  (snap the reference blueprint's proposed position, `HandleMovement`, `ApplySnapResultTime`).
- `BacSelectionHandler : EditorSelectionHandler` — `HandleMovement` converts the screen x-delta to a
  degree delta and adds it (mod 360) to each selected object's `AngleDeg`; no clamping needed since the
  axis wraps (simpler than mania's column clamping). Shoulder notes ignore the angular component (they
  only move in time) in v1.

### Test scene

`TestSceneBacEditor : EditorTestScene` in the Tests project (`osu.Game/Tests/Visual/EditorTestScene.cs`
is the base; follow its `CreateRuleset`-style override). Start from an empty/near-empty beatmap and place
objects by hand; `BacTestBeatmapGenerator` can seed content later if useful.

## Milestones (each independently runnable via the test browser)

1. **Skeleton:** model setters; composer + editor drawable ruleset + empty `BacEditorPlayfield` with
   angle grid + beat snap grid; Select tool only; `TestSceneBacEditor` opens and scrolls.
2. **Cardinal notes end-to-end:** editor drawable, placement tool, selection blueprint, move/delete,
   angle-snap toolbox setting. This proves the whole snap → blueprint → EditorBeatmap pipeline.
3. **Remaining simple objects:** Hold (duration drag), Shoulder (strips), CenterSlam/EdgeSlam (arrow
   sprites).
4. **Ghost bands:** wrapping in the snap function, drawable ghost twins, blueprint twin hit-testing.
5. **Sliders:** polyline editor drawable, multi-click placement, node drag + `T` insert.
6. *(Follow-up, out of v1 scope)*: legacy-.osu encode/decode roundtrip so maps save/reload; drawables for
   slams in gameplay; angle-snap persistence in ruleset config.

## Verification

- `dotnet build osu.Game.Rulesets.BigAssCircle/osu.Game.Rulesets.BigAssCircle.csproj` after each step.
- Run the Tests project (VisualTestRunner → OsuTestBrowser) → `TestSceneBacEditor`; per milestone:
  place each object type, drag it in angle and time, verify snap increments (45° default, changeable),
  drag across a band edge and confirm the wrap, select via a ghost twin, undo/redo (Ctrl+Z) after each
  operation (exercises `EditorBeatmap` change transactions), delete via right-click.
- Runtime exceptions land in `%APPDATA%\osu\logs\*.runtime.log`.

## Risks / gotchas carried over from gameplay work

- Placement blueprints construct their `HitObject` up front (framework pattern) — needs the settable
  `AngleDeg` (placeholder 0 at construction).
- Nested objects (`SliderChild`, `HoldNoteHead`) must regenerate on edit — always mutate then
  `EditorBeatmap.Update(h)`; never cache nested drawable state across path edits.
- Editor drawables must not expire on "hit" (autoplay runs under the composer) — no-op
  `UpdateHitStateTransforms`, mirroring mania's `EditorColumn` fix.
- Keep all angle↔x math in the single mapping helper; ad-hoc conversions are how wrap bugs will creep in.

## Slider crossover (wrap-around rendering) — post-v1 feature

A slider's polyline lives in *unwrapped* angle space (`RotationOffset` is unbounded), so a sweep that
crosses the grid's wrap seam currently runs off the edge and vanishes. It must instead re-enter from the
opposite edge and continue — including arbitrarily many full turns (multi-turn sliders are legal for
mapping; no clamping, no warnings needed if rendering handles them).

Decisions (agreed):
1. **Multi-turn supported outright** via the copies loop below — no special-casing, no warning.
2. **One drag handle per node**, placed at the node's *wrapped* (on-grid) position; copies are visual only.
3. **Body strip / selection quad unchanged** — selection stays anchored at the head.

Approach — generalise the ghost-twin idea from "±1 copy of a point" to "k copies of an extent":

- **`EditorAngleMapping.VisibleWrapCopies(minGridDeg, maxGridDeg)`** (pure, unit-tested): the set of
  integers k for which the range shifted by −k·360 intersects the visible window
  `[−GHOST_DEGREES, 360 + GHOST_DEGREES]`. A fully on-grid slider yields {0}; a seam-crosser {0, 1} (or
  {−1, 0}); each extra full turn adds one more k.
- **`SliderPolylineVisual`**: compute the vertex list once (as today), take its unwrapped grid-degree
  extent (body grid degrees + min/max node offset), and draw one path + node-marker copy per k, offset by
  −k·360·pxPerDeg. The rebuild early-out must also compare the copy set — dragging the *body* toward the
  seam changes the copies while leaving the (body-relative) vertices identical.
- **`EditorDrawableSliderBody.TwinXFraction() => null`** — the polyline now draws its own wrap copies;
  the base ghost twin would duplicate the k=±1 copy.
- **`SliderPlacementBlueprint`**: preview path gets the same copy rendering. Also fix the rubber-band
  vertex: it currently uses the raw cursor x, but the committed node uses `MinimalDiff` — so previewing
  across the seam draws a long wrong-way line. Place the rubber vertex at the *unwrapped* continuation
  (`lastNodeX + MinimalDiff(lastAbsolute, cursorAngle)·pxPerDeg`) and let the copies make it visible at
  the cursor.
- **`SliderSelectionBlueprint`**: node handle x = wrapped node angle via `ToX`, positioned relative to
  the body's x — no longer the raw unwrapped offset. `dragNode` already works across the seam
  (`MinimalDiff` from the current absolute).

Verification: NUnit tests for `VisibleWrapCopies`; editor scene test placing a seam-crossing slider
(assert `RotationOffset` takes the short way and the polyline renders 2 copies — internals are visible
to the test project); manual multi-turn slider in the test browser.
