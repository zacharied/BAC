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
  gives East=0, North=π/2, West=π, South=3π/2.
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
- The container's alive-loop only *positions* `DrawableCardinalNote` (it skips everything else). **Paths
  self-manage their geometry each frame** in `DrawableSliderBody.updatePath`.

## Hit objects

`BacHitObject : HitObject` is the abstract base (`Objects/`); it seeds a soft hit-normal sample and its
`CreateJudgement()` returns a plain `Judgement` (`MaxResult = Perfect`, `MinResult = Miss`). Two families:

- **Notes** (single timed presses). `Note : BacHitObject` (abstract) exposes a `ButtonInput`; its drawable
  `DrawableNote<T>` is an `IKeyBindingHandler<BigAssCircleAction>`.
  - `CardinalNote` — a hit in one of four `CardinalDirection`s (→ `ButtonE/N/W/S`). Drawn by
    `DrawableCardinalNote` (a "square" sprite with spawn/hit/miss transforms). Both note drawables back
    note-lock via `IHittableNote.MissForcefully()` (implemented on the `DrawableNote<T>` base).
  - `ShoulderNote` — a hit on one `HorizontalDirection` side (→ `ButtonL/R`). Its `Direction` maps
    Left→West / Right→East (via `IHasCardinalDirection`) purely for *positioning* — it rides its **own**
    lane (one per side), separate from the cardinal West/East lanes, so shoulder and cardinal note-lock
    never interfere. Drawn by `DrawableShoulderNote` — the curved **paddle** sprite, rotated to face its
    angle (180° for West) and auto-sized so its curvature radius matches the ring (`height = ScrollLength`,
    since the art's curve radius ≈ its own height).
- **Sliders** (analog-stick held objects). `SliderBody : BacHitObject, IHasDuration` — a `Side`
  (`HorizontalDirection`), a `BacPath` (`DirectionDeg` = initial angle + control points), and
  `Duration`/`EndTime` derived from the furthest-in-time control point. It nests one `SliderHead` (the
  start node) and one `SliderChild` per control point. Drawn by `DrawableSliderBody` — see "The path
  system" and "Slider edge animation".
- `BacSlamEdge : BacHitObject` (`Side`, `RotationalDirection`, `Angle`) exists as a model but has no
  drawable yet.

Top-level drawable dispatch: `DrawableBigAssCircleRuleset.CreateDrawableRepresentation` maps
`SliderBody → DrawableSliderBody` and `CardinalNote → DrawableCardinalNote`. `SliderHead`/`SliderChild`/
`ShoulderNote` are nested and created by their parents' `CreateNestedHitObject`, not dispatched here.

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

A path = **start node** `(angle = DirectionDeg, time = StartTime)` + control points (`BacPath` holds a
`BindableList<BacPathControlPoint>`, in `Core/Path/`). Control point `i` defines **node `i+1`** at
`(DirectionDeg + RotationOffset, StartTime + TimeOffset)`. It renders as a subdivided polyline in polar
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
  Objects/            hit objects (BacHitObject; Note/CardinalNote/ShoulderNote; SliderBody/Head/Child; BacSlamEdge)
  Objects/Drawables/  drawable representations (DrawableCardinalNote, DrawableSliderBody/Head/Child, …)
  Objects/Judgement/  slider judgement result / hit windows
  Core/               enums (CardinalDirection, HorizontalDirection, RotationalDirection)
  Core/Path/          BacPath, BacPathControlPoint
  Input/              AnalogInputManager (+ SliderCatcher)
  UI/                 BigAssCirclePlayfield → Ring → Lane, scrolling container, Arc (the ring),
                      BacOrderedHitPolicy (note-lock), PlayfieldKeybeam, StickIndicator, DrawableRuleset
  Beatmaps/           BigAssCircleBeatmapConverter (osu → this ruleset), BacTestBeatmapGenerator
osu.Game.Rulesets.BigAssCircle.Tests/   TestSceneOsuPlayer, VisualTestRunner
```
