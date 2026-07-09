# BigAssCircle — agent onboarding

A custom osu!(lazer) ruleset plugin (C#, osu!framework / osu.Game). It is a rhythm-based action game in which the player times inputs to match onscreen prompts (the "hit objects"). Hit objects spawn at the centre of the screen and move outward in a given direction (the object's "angle") toward the edge of the circle. An object reaching the outer edge is the "judgement", and almost always coincides with an element of the background music.

This doc is the shared mental model — keep it updated as the codebase grows.

## Code style

Nullability is enabled. For BigAssCircle classes, assume objects are non-null unless explicitly marked nullable. For DI-resolved fields (usually `[Resolved]`), initialise to `null!` to avoid warnings.

## Build / run / test

- **Build:** `dotnet build osu.Game.Rulesets.BigAssCircle/osu.Game.Rulesets.BigAssCircle.csproj`
- **Visual tests:** run the `osu.Game.Rulesets.BigAssCircle.Tests` project (VisualTestRunner → OsuTestBrowser).
  `TestSceneOsuPlayer` builds its beatmap from `BacTestBeatmapGenerator.GenerateBeatmap()`, which
  hand-authors sliders and cardinal notes in code — the fastest way to try a path.
- **Runtime logs (exceptions, etc.):** `%APPDATA%\osu\logs\*.runtime.log` (test host name is `osu`).
  Newest by mtime = most recent run; an unhandled gameplay exception lands here.

## Core rendering model (learn this first)

- **Polar coordinate system** centred on the playfield. Convention everywhere (notes, `Arc`, paths):
  `x = cos(θ)·r`, `y = -sin(θ)·r` (screen y is down). So `θ = 0` points **right** (East) and θ increases
  **counter-clockwise**: East→right, North→up, West→left, South→down. `CardinalDirection.ToRadians()`
  gives East=0, North=π/2, West=π, South=3π/2 (`.ToDegrees()` gives 0/90/180/270). Angle/radian
  conversion lives in `MathUtils` (`DegToRad`/`RadToDeg`).
- **Every positioned object carries an arbitrary angle via `IHasAngle` (`int AngleDeg`).** This is the
  authority for *where* an object is painted — objects are no longer pinned to four cardinal lanes; an
  angle can be anything `[0, 360)`. `PositionAtTime` reads `AngleDeg` off `IHasAngle` (falling back to 0).
  Cardinal `Direction` is now *derived* from the angle (`CardinalDirection.FromAngle`, which normalises
  and rounds to the **nearest** cardinal) and only governs **which button/lane** the note belongs to — so
  angle drives position, nearest quadrant drives input/note-lock. `SliderBody`/`Head`/`Child` and both note types implement
  `IHasAngle`; slider nodes derive theirs as `SliderBody.AngleDeg + RotationOffset`.
- The **scroll algorithm maps TIME → RADIUS**: an object sits at the centre one `TimeRange` before its
  time and reaches the ring (`ScrollLength = min(width,height)/2`) exactly at its time — that is why
  things emerge from the centre.
- **`BigAssCircleScrollingHitObjectContainer`** owns this mapping. Public API:
  - `ProgressAtTime(time)` — distance clamped to `[0, ScrollLength]`. Used by notes.
  - `DistanceFromCentreAtTime(time)` — **unclamped** (negative = not yet emerged, `> ScrollLength` =
    already consumed by the ring). Used by paths so they can clip against both boundaries.
  - `ScrollLength` — ring radius.
  - It is `[Cached]`, and there are **several instances** — one per `Lane` (holding that direction's
    notes) plus one on the `Ring` (holding cross-lane paths). All are full-size and compute identical
    geometry. A `DrawableSliderBody` `[Resolved]`s the nearest one (the ring's), which is why paths must
    live in the ring's container, not a lane's. See "Playfield structure".
- The container's alive-loop point-positions every drawable whose hit object is `IHasAngle`, **except
  those whose drawable implements `ISelfPosition`** (`Objects/Drawables/`) — the marker for "I compute my
  own geometry each frame." The slider drawables (`DrawableSliderBody`/`Head`/`Child`) carry it, so
  **paths self-manage their geometry** in `DrawableSliderBody.updatePath` while notes get point-positioned.
  Any new point-positioned object just needs `IHasAngle`; any new self-managing one adds `ISelfPosition`.

## Hit objects

`BacHitObject : HitObject` is the abstract base (`Objects/`); it seeds a soft hit-normal sample and its
`CreateJudgement()` returns a plain `Judgement` (`MaxResult = Perfect`, `MinResult = Miss`). Two families:

- **Notes** (single timed presses). `Note : BacHitObject` (abstract) exposes a `ButtonInput`; its drawable
  `DrawableNote<T>` is an `IKeyBindingHandler<BigAssCircleAction>`.
  - `CardinalNote` — carries a raw `AngleDeg` (`IHasAngle`) that sets its painted position; its
    `Direction` is *derived* (`CardinalDirection.FromAngle`) and only selects the button (→ `ButtonE/N/W/S`)
    and note-lock lane. Drawn by `DrawableCardinalNote` (a "square" sprite with spawn/hit/miss transforms;
    the sprite is not rotated to the angle). Both note drawables back note-lock via
    `IHittableNote.MissForcefully()` (implemented on the `DrawableNote<T>` base).
  - `ShoulderNote` — a hit on one `HorizontalDirection` `Side` (→ `ButtonL/R`). `AngleDeg` comes from
    `Side.ToAngleDeg()` (Right→0/East, Left→180/West) and its derived `Direction` (via
    `IHasCardinalDirection`) picks the lane — it rides its **own** lane (one per side), separate from the
    cardinal West/East lanes, so shoulder and cardinal note-lock never interfere. Drawn by
    `DrawableShoulderNote` — the curved **paddle** sprite, rotated to face its angle (180° for West) and
    auto-sized so its curvature radius matches the ring (`height = ScrollLength`, since the art's curve
    radius ≈ its own height).
  - `HoldNote` — a `CardinalNote` with a `Duration` (`Note, IHasCardinalDirection, IHasAngle, IHasDuration`);
    rides the same cardinal lane as a `CardinalNote` at its angle. Drawn by `DrawableHoldNote` (an
    `ISelfPosition` object like the slider body): a square head sprite plus a straight radial **trailing
    line** (a two-vertex `SmoothPath` from the head at `StartTime` inward to the tail at `EndTime` — the tail
    is *closer to the centre* because it is later in time; a plain radial clamp to `[0, ScrollLength]` is
    correct here since there is no swept contact point). **Judgement is two-part:** it nests a
    `HoldNoteHead` (angle/button from the parent) judged exactly like a cardinal note; the parent hold owns
    input (delegating the head press via `DrawableHoldNoteHead.UpdateResult()`) and judges the **tail** with
    `DrawableSliderChild`'s time-accumulation over `[StartTime, EndTime]` (`holdPresses > 0` per frame → caught
    runs, independent of the head), graded finely (top tier ≥99% caught). The tail result is **deferred until
    the head is judged** (`CheckForResult` no-ops until `Head.Judged`). **Head carryover is short-hold-only:**
    only when `Duration < head miss window` (no meaningful body) does a missed head fail the whole hold and the
    head's grade cap the result / a body-less hold inherit it. For normal-length holds the body is judged purely
    on how much was held, so missing the head only costs the head's own judgement — it never fails the hold.
    Re-grabbable (release/press just flips `holdPresses`); the trail greys (`Colour = Gray`) while active but not
    held. `MissForcefully()` is a **no-op** (the hold always self-resolves at the tail, so note-lock must not
    nuke it).
- **Sliders** (analog-stick held objects). `SliderBody : BacHitObject, IHasDuration, IHasAngle` — a `Side`
  (`HorizontalDirection`), an `AngleDeg` (initial angle; each `BacPathControlPoint.RotationOffset` is
  relative to it), a `BacPath`, and `Duration`/`EndTime` derived from the furthest-in-time control point.
  It nests one `SliderHead` (angle = body's) and one `SliderChild` per control point (angle =
  `body.AngleDeg + RotationOffset`); both are `IHasAngle` and take the body via constructor. Drawn by
  `DrawableSliderBody` — see "The path system" and "Slider edge animation".
- `BacSlamEdge` / `BacSlamCentered` — `BacHitObject, IHasAngle` (`Side`, `AngleDeg`; edge also
  `RotationalDirection`) exist as models but have no drawables yet.

Top-level drawable dispatch: `DrawableBigAssCircleRuleset.CreateDrawableRepresentation` maps
`SliderBody → DrawableSliderBody`, `CardinalNote → DrawableCardinalNote`, `HoldNote → DrawableHoldNote` and
`ShoulderNote → DrawableShoulderNote`. `SliderHead`/`SliderChild`/`HoldNoteHead` are nested and created by
their parents' `CreateNestedHitObject`, not dispatched here.

Every drawable derives from `DrawableBacHitObject<T> : DrawableHitObject<BacHitObject>` (with a `new T
HitObject` shadow). Because it is typed to the **base** `BacHitObject` (generic drawables are invariant),
any `DrawableBacHitObject<T>` satisfies `CreateDrawableRepresentation`'s `DrawableHitObject<BacHitObject>`
return type.

## Input

- **Buttons.** `BigAssCircleAction` is the physical action set: each cardinal direction has two actions —
  a d-pad (`…1`) and a face button (`…2`) — plus `ButtonL`/`ButtonR`. `ToButtonInput()` collapses an
  action onto the logical `BigAssCircleButtonInput` (`ButtonE/N/W/S/L/R`) that a `Note` matches against;
  `ToCardinalDirection()` maps to a direction. `BigAssCircleInputManager` binds with
  `SimultaneousBindingMode.All`.
- **Analog sticks.** The `[Cached]` `AnalogInputManager` (`Input/`) owns two `SliderCatcher`s (left stick
  / right stick) — see "Sliders: analog catch input & judgement".

## Playfield structure: ring & lanes

Three levels, analogous to mania's `ManiaPlayfield → Stage → Column`, giving **lane-independent
note-lock**:

- **`BigAssCirclePlayfield`** — thin top-level router. Nests one `Ring`, forwards `Add`/`Remove` to it,
  caches the `AnalogInputManager`, and holds the global overlays not tied to a lane (the two
  `StickIndicator`s and a temporary `JoystickDebugOverlay`).
- **`Ring`** — the circular arena. Owns the shared furniture in z-order (radial lines behind, its own
  hit-object container for cross-lane **paths**, the four `Lane`s, then the outer `Arc` in front). Its
  container is also the geometry authority a path resolves. `laneFor(hitObject)` routes by **type**:
  `CardinalNote → cardinalLanes[(int)Direction]`, `ShoulderNote → shoulderLanes[side]` (its own lane so
  note-lock is independent), everything else (sliders) → the ring's own container. **All lanes are created
  in the constructor**, so a routed `Add` never hits a null lane.
- **`Lane`** — the generic note-lock column. Owns its own hit-object container (its notes) and a
  `BacOrderedHitPolicy`, and optionally a `PlayfieldKeybeam` (the four cardinal lanes pass one; the two
  shoulder lanes pass `null`, since L/R has no cardinal keybeam). Wires `CheckHittable` onto its
  `IHittableNote`s (the non-generic view over `DrawableNote<T>`) in `OnNewDrawableHitObject` and
  force-misses skipped earlier notes in `onNewResult`. All lanes are full-size and share the polar centre —
  a "lane" is a logical grouping, not a spatial region.

**Why it's lane-independent:** each lane's hit policy only ever sees *its own* container's `AliveObjects`,
so `GetNext`/note-lock is naturally scoped per direction. Paths are cross-directional, so they live in the
ring's container, not a lane.

**Note-lock + input ordering (read before touching `OnPressed`):**

- `DrawableNote.OnPressed` **must `return UpdateResult(true)`** when it lands a hit — i.e. consume the
  press. Returning `false` lets one tap fall through and hit *every* in-window note in the lane, defeating
  note-lock.
- `HitObjectContainer.Compare` orders **earlier objects to the front of the input queue**, so the earliest
  hittable note is the one that consumes.
- Because a hit consumes the press, the **keybeam must observe it first**. `Lane` draws the keybeam at the
  back via `CreateProxy()` but adds the real drawable *in front* of the hit objects for input; it returns
  `false`, then the note consumes. Draw order and input order are deliberately decoupled.
- `BacOrderedHitPolicy.IsHittable` uses the **lenient** rule (`next == null || time < next.StartTime`)
  plus consumption plus `HandleHit` force-miss (`MissForcefully`) — **not** a strict "only the earliest is
  hittable" gate, which would let a pending *missed* note block the next one until it auto-misses.

## The path system (most hard-won detail lives here)

A path = **start node** `(angle = SliderBody.AngleDeg, time = StartTime)` + control points (`BacPath` holds
a `BindableList<BacPathControlPoint>`, in `Core/Path/`). Control point `i` defines **node `i+1`** at
`(AngleDeg + RotationOffset, StartTime + TimeOffset)`. It renders as a subdivided polyline in polar
space (`DrawableSliderBody.updatePath`), delegating to `SmoothPath` for thickness / joints / AA.

- **Radius is linear-in-time per link.** Each link is clipped to the visible band with Liang–Barsky
  (`clipToBand(rA, rB, innerRadius, outerRadius, …)`). The inner crossing is the **emergence front**
  creeping out from the centre; the outer crossing is **consumption** at the edge. The ring-contact point
  slides continuously along the curve — **never clamp radius** (clamping pins the contact and breaks both
  emergence and the sweep).
- **Angle interpolation is per-link**, chosen by the control point that *ends* that link (**a control
  point governs the segment leading into it**; the start node governs nothing):
  - **Linear** (default) — exact authored geometry; constant velocity per segment; velocity jumps at nodes.
  - **`SweepEasing`** (`Easing` enum) — eases the *angle's* progress only; radius stays linear so
    **timing is unchanged**; no overshoot/anticipation. `InOut*` eases through nodes (slight settle).
    Back/Elastic/Bounce intentionally overshoot.
  - **`Smooth`** (Catmull-Rom) — C1-continuous sweep, but tangents come from *neighbouring* nodes, so it
    **bends the geometry** (anticipation/overshoot) and is only C1 where adjacent links are also smoothed.
  - Tradeoff: with no look-ahead and exact node angles, the only velocity two segments can share at a
    node is zero — so smoothness without geometry change (easing) implies a momentary settle at nodes.
- **Rendering plumbing:** `renderBand(inner, outer, alpha, pool, container, idx)` walks every link, clips
  it to a radial band, and emits contiguous runs into a pooled `SmoothPath` list. A run breaks — and a
  fresh path starts — wherever the visible portion is not continuous, so gaps aren't bridged by a stray
  line. Two pools/containers: `bodyPaths`/`pathContainer` (the body, glow-wrapped) and
  `escapePaths`/`escapeContainer` (the beyond-ring band).

## Sliders: analog catch input & judgement

Sliders are judged with an **analog stick**, not taps. The `AnalogInputManager` owns two `SliderCatcher`s,
one per thumbstick:

- **`SliderCatcher`** — one per `HorizontalDirection` (Left = left stick, Right = right stick). Each
  `OnJoystickAxisMove` sets `Angle = Atan2(-y, x)` (polar convention, range (-π, π]) and `Activated`
  (stick pushed past `DEADZONE` = 0.4). `Size` is the catch-arc width (`SizeDeg` = 72° → radians).
  **`IsCatchingAt(int angleDeg)`** is true iff `Activated` and the *wrap-safe* angular distance from
  `Angle` to the target is `< Size/2`. `StickIndicator` draws each catcher's arc at `RadiusScale = 1.06`
  (just outside the ring).
- **Angle authority — `DrawableSliderBody.AngleDegAt(double time)`:** the body's angle at a time, matching
  the *rendered* swept geometry (same per-link easing/smoothing via `thetaAt`). It's the angle a catcher
  must point at to be catching the body there, and lives on the drawable (not the model) because the node
  arrays / interpolation do. Since time→radius is linear per link, the point currently *at the ring* has
  node-time == now, so `AngleDegAt(Time.Current)` is the **leading edge's** angle.
- **Per-node judgement — `SliderChild` / `DrawableSliderChild`:** each control point is a node, judged on
  the **fraction of its segment window it was caught.** The window runs from the previous node to the
  child's node — `SliderBody.GetSegmentStartTime(child)` returns the previous-node time (head at
  `StartTime` for the first control point; `IndexOf` is by reference). Each frame within
  `[segmentStart, StartTime]`, `DrawableSliderChild` samples `IsCatchingAt(AngleDegAt(now))` and
  accumulates `Time.Elapsed` into runs of constant catch state (`CatchRecord`). At the node time,
  `CheckForResult` maps the caught fraction with a **binary threshold** (`>= 0.5` → `MaxResult`/Perfect,
  else `MinResult`/Miss). Constraints: forward-play only (accumulates `Time.Elapsed`; the
  resolved-but-unused `IGameplayClock` is the seam for a rewind-safe rewrite); window edges are
  frame-granular; pooled state is cleared in `OnFree`.

`SliderHead` currently applies a max result unconditionally (todo), and `SliderBody` itself is unjudged
(`CheckForResult` is a no-op).

## Slider edge animation (consume vs. escape)

`DrawableSliderBody.updatePath` renders the body in two radial zones each frame. Beyond the ring, the look
depends on whether the **leading edge is being caught right now** — `isLeadingEdgeCaught()` =
`IsCatchingAt(AngleDegAt(Time.Current))` (per-frame, so it can flicker if the stick wobbles):

- **Main body `[0, ScrollLength]`** — full alpha, glow-wrapped.
- **Escape band `[ScrollLength, ScrollLength × 1.06]`** (1.06 = catcher radius, `catcher_radius_scale`,
  mirroring `StickIndicator.RadiusScale`):
  - **Caught → consumed.** Solid out to the catcher radius, and a **pulsating, spinning `Box`** (`tipBox`,
    side-coloured) rides the leading tip. `tryGetLeadingTip` finds the outermost visible point in the band
    (radius is monotonic per link, so the clipped sub-range endpoints are the extremes); the tip is in the
    same polar-centred space as the path vertices.
  - **Uncaught → escaping.** Fades out over the band, hard-clipped at 1.06. `SmoothPath` has one uniform
    colour (no length-wise gradient), so the fade is built from **`escape_bands` overlapping layers** that
    all start at the ring and reach progressively further, each at `escape_layer_alpha`; composited
    source-over, inner radii (covered by every layer) are near-opaque and the rim (one layer) is faint.
    **Do not slice the band into short disjoint pieces** — a slice shorter than `PathRadius` renders as a
    rounded-cap blob; overlapping layers keep caps only at the graduated tips.

The animation is purely visual and independent of the per-child judgement.

## The editor (Edit/)

A mania-style compose screen (spec: `PLAN-editor.md`, plan: `PLAN-editor-impl.md`), entered via
`BigAssCircleRuleset.CreateHitObjectComposer()` → `BigAssCircleHitObjectComposer :
ScrollingHitObjectComposer<BacHitObject>`. It is **entirely separate from the gameplay presentation**: a
second drawable ruleset (`DrawableBigAssCircleEditorRuleset : DrawableScrollingRuleset`, vertical
`ScrollingDirection.Down`, constant algorithm, scroll speed synced to timeline zoom) hosts a rectangular
`BacEditorPlayfield : ScrollingPlayfield` where **y = time** (stock scrolling container) and **x = the
circle unrolled**.

- **All angle↔x maths lives in `EditorAngleMapping`** — never do it ad hoc. Left grid edge =
  `ANGLE_ORIGIN` = the North/West quadrant boundary (135°), chosen so the wrap seam lands on a diagonal
  and no cardinal lane is split across it; angle increases CCW rightward, so the cardinal lanes read
  West, South, East, North left-to-right (each centred a margin inside the grid). Each side has a 30° **ghost band**
  (`GHOST_DEGREES`), so the full width spans `TOTAL_DEGREES` = 420 and every x is a fraction of that.
  `SnapX` snaps in the *unwrapped band domain* (a cursor in a band stays put visually) and returns the
  wrap-normalised angle; `GhostTwinX` says where an object's band clone sits; `MinimalDiff` picks the
  shortest signed rotation (slider node offsets must not spin the long way).
- **Snapping:** blueprints call `composer.FindSnappedAngleTimeAndPosition` — base
  `FindSnappedPositionAndTime` for beat-snapped time, then angle snapping (toolbox-configurable
  `AngleSnap`, default 45°) on top, returning a `BacSnapResult` carrying `AngleDeg`. **Gotcha:** the base
  scrolling snap *recentres x to the playfield middle* (`ScreenSpacePositionAtTime` uses `DrawWidth / 2`)
  — take the angle from the **original** cursor position, only y/time from the base result.
- **Models are mutable for editing:** `IHasMutableAngle : IHasAngle` (settable `AngleDeg`) on
  CardinalNote/HoldNote/SliderBody/both slams; `ShoulderNote.Side` settable instead (its angle stays
  derived). Mutate, then `EditorBeatmap.Update(h)` — that re-runs ApplyDefaults and regenerates nested
  objects (slider children, hold heads).
- **Editor drawables** (`Edit/Drawables/`, base `EditorDrawableBacHitObject<T>`): x from `AngleDeg` every
  frame (`ComputeXFraction`, overridden by shoulder notes to sit in their **lane strips at the quadrant
  boundaries** — Left at 225°, Right at 45°); y/height from the scrolling container (`IHasDuration` ⇒
  height = duration length; bottom origin, grows upward). They auto-judge as time passes (hitsound
  feedback) but `UpdateHitStateTransforms` is a no-op so nothing fades while editing. Visuals come from
  `CreateVisual()` so the base can instantiate a **second copy as the ghost twin** when
  `GhostTwinX(angle)` is non-null. Nested objects get `EditorDrawableNestedStub` (invisible; still needs
  the nested-container plumbing per the gotcha below). The slider draws as `SliderPolylineVisual` — nodes
  at raw `RotationOffset` (unwrapped, so a path can sweep past the grid edges — multi-turn is legal), and
  the polyline is drawn once per **wrap copy** (`EditorAngleMapping.VisibleWrapCopies`, the ghost-twin
  idea generalised from a point to an extent, offset −k·360° each) so a seam-crossing path re-enters from
  the opposite edge; the slider drawable's own `TwinXFraction()` is null (copies replace the base twin).
  The rebuild early-out compares vertices AND the copy set (dragging the body toward the seam changes
  copies while body-relative vertices stay identical). Node drag handles sit at each node's *wrapped*
  on-grid position (one handle per node; copies are visual only), and the placement rubber-band previews
  at the unwrapped `MinimalDiff` continuation (what the commit would produce), not the raw cursor x.
  Paths are positioned with `path.Position = -path.PositionInBoundingBox(Vector2.Zero)`.
- **Slider path rendering perf (learn before touching slider visuals).** `Path`/`SmoothPath` is an
  `IBufferedDrawable`: **each instance owns a framebuffer** sized to its vertex bounds, so **never `new` a
  path per frame** — recreating them each `Update` allocated a fresh (up to window-sized) framebuffer
  every frame and ran memory into the tens of GB once a wide seam-crossing path existed. Both draw paths
  are **pooled/reused**: `SliderPolylineVisual` reconciles a `PathCopy` pool (buffered path + node dots
  per wrap copy, geometry set in place, surplus hidden), and `SliderPlacementBlueprint` reuses a
  `SmoothPath` pool (`ClearVertices()` on the unused ones). A `GlobalStatistic<int>` **"Slider polyline
  rebuilds"** counter (visible in the Ctrl+F2 overlay) stays as a diagnostic — it should not advance while
  a slider merely sits.
- **Slider lines are clipped to the timeline by MASKING, not vertex clipping.** Wrap copies extend past
  the grid edges; they're kept inside the timeline by `Masking = true` on the `HitObjectContainer`'s
  wrapper (committed sliders) and on the placement preview container. The ghost bands lie *within* the
  bounds so their clones still show. This is purely cosmetic: `BufferedDrawNode` already clips each path's
  framebuffer to the **root node** (the whole window, `clipToRootNode: true`), so an ancestor mask does
  **not** shrink the framebuffer. Actually reducing a wide path's framebuffer needs per-segment vertex
  clipping against the x-band (as the gameplay path does) — deferred.
- **Blueprints** (`Edit/Blueprints/`): placement base `BacPlacementBlueprint<T>` writes snapped
  angle+time while `Waiting`; `InstantPlacementBlueprint` (cardinal/slams) places on click; hold = mania's
  click-drag-release duration pattern; shoulder picks the `Side` whose strip is angularly nearer;
  slider = **multi-click** (click body, click each node — must advance in time — right-click commits,
  ≥1 node required, `T` inserts a node at the cursor on a *selected* slider). Selection base
  `BacSelectionBlueprint<T>` re-positions per frame and makes ghost twins **interactable** by drawing a
  twin outline and hit-testing via *translating the query point back onto the main copy* in
  `ReceivePositionalInputAt`. `ReplacesExistingObject` is scoped to same-angle (same-side for shoulders) —
  the framework default would delete *any* object sharing the beat.
- **What selects an object vs. what the yellow box is (don't confuse them).** Click-to-select is driven
  entirely by `blueprint.IsHovered` ⇐ **`ReceivePositionalInputAt`**; drag-box select uses the single
  `ScreenSpaceSelectionPoint`. The **yellow box is the framework `SelectionBox`** (a bordered `Container`
  with the rotate/scale handles) sized to the `RectangleF.Union` of every selected blueprint's
  **`SelectionQuad.AABBFloat`** — it is *necessarily an axis-aligned rectangle* and it **never** drives
  selection. So `SliderSelectionBlueprint` is **path-precise**: it traces a yellow outline over the actual
  polyline (unwrapped `RotationOffset` + wrap copies, pooled `SmoothPath`s kept current every frame even
  while deselected because they *are* the hit-test surface — `outline_radius` = both thickness and click
  tolerance), and overrides `ReceivePositionalInputAt` to report **only** those paths + node handles — so
  clicking a segment/node selects but clicking empty space inside the bounding box falls through. Its
  `SelectionQuad` bounds the primary (k=0) polyline purely to size the handle box; that can't cause false
  clicks. `TwinXFraction()` is null (wrap copies cover the seam). To follow an object's shape, give the
  blueprint a shaped outline + precise `ReceivePositionalInputAt` — never try to make the `SelectionBox`
  itself a polygon.
- **Selection movement** (`BacSelectionHandler.HandleMovement`) converts the x screen-delta to whole
  degrees and rotates every selected `IHasMutableAngle` (mod 360 — no clamping; the axis wraps). Edge slam
  direction is a ternary context-menu item ("Anticlockwise").
- **Tests:** `TestSceneBacEditor : EditorTestScene` drives the real editor headlessly with
  `InputManager` clicks — the fastest way to verify editor behaviour (`dotnet test --filter
  TestSceneBacEditor`). Note placement auto-seeks the clock to the placed object (objects land on the
  judgement line at the bottom), so tests wait for the seek and target `screenPositionOf(hitObject)`.
- **Not yet done:** saving (the legacy `.osu` encoder can't roundtrip BAC objects — needs a legacy-field
  mapping), slider `Side` selection in the editor (always Left), hold/slider twin drag handles (main copy
  only), z-ordering sliders above notes, per-segment vertex clipping of slider paths (only masked for now,
  so wide seam-crossing paths still allocate large framebuffers).

## Gotchas / patterns (each cost a debugging cycle)

- **Nested hit objects need a drawable AND a home in the tree.** `CreateNestedHitObject` must return
  non-null for every nested object (else `CreateNestedHitObject returned null …`). *And* the nested
  drawables must be added to a container in the tree (`AddNestedHitObject` → `nestedContainer`), or they
  never receive a clock and crash with an NRE in `OnKilled`/`UpdateResult`.
- **Paths self-position; the scrolling container's loop skips them.** The alive-loop only positions
  `DrawableCardinalNote`; a blanket cast would crash on paths, which compute their own geometry in
  `updatePath`.
- **Gameplay-affecting = opt-in.** Anything that changes a path's shape/feel must default to the existing
  exact behaviour and be opt-in per node/path (`Smooth`/`SweepEasing` default off).

## Layout

```
osu.Game.Rulesets.BigAssCircle/
  Objects/            hit objects (BacHitObject; IHasAngle; Note/CardinalNote/ShoulderNote; SliderBody/Head/Child; BacSlamEdge/Centered)
  Objects/Drawables/  drawable representations (DrawableCardinalNote, DrawableSliderBody/Head/Child, …)
  Objects/Judgement/  slider judgement result / hit windows
  Core/               enums + extensions (CardinalDirection, HorizontalDirection, RotationalDirection)
  Core/Path/          BacPath, BacPathControlPoint
  MathUtils/          MathUtils (DegToRad / RadToDeg helpers)
  Input/              AnalogInputManager (+ SliderCatcher)
  UI/                 BigAssCirclePlayfield → Ring → Lane, scrolling container, Arc (the ring),
                      BacOrderedHitPolicy (note-lock), PlayfieldKeybeam, StickIndicator, DrawableRuleset
  Edit/               the editor: composer, editor drawable ruleset + playfield, EditorAngleMapping,
                      composition tools, BacBlueprintContainer/BacSelectionHandler
  Edit/Drawables/     editor timeline representations (EditorDrawableBacHitObject<T> + per-type visuals)
  Edit/Blueprints/    placement/selection blueprints (+ Components/ drag pieces, outline pieces)
  Beatmaps/           BigAssCircleBeatmapConverter (osu → this ruleset), BacTestBeatmapGenerator
osu.Game.Rulesets.BigAssCircle.Tests/   TestSceneOsuPlayer, TestSceneBacEditor, VisualTestRunner
```
