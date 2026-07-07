# BigAssCircle — agent onboarding

A custom osu!(lazer) ruleset plugin (C#, osu!framework / osu.Game). It is a rhythm-based action game in which players must time specific inputs to match onscreen prompts (the "hit objects"). These hit objects are presented to the player as sprites that spawn at the center of the screen and move in a given direction (referred to as the object's "angle") towards the edge of the circle. An object arriving at the outer edge of the circle is called the "judgement" or "judgment" and almost always coincides with an element of the background music.

This doc is the shared mental model — keep it updated as the codebase grows.

## Code style

Nullability (the `?` operator/type modifier) are enabled in this project. When working with BigAssCircle classes, assume objects are non-null unless they are explicitly marked as nullable.

In the case of DI-resolved classes (usually marked with `[Resolved]`) explicitly initialize them to `null!` to avoid warnings.

## Build / run / test

- **Build:** `dotnet build osu.Game.Rulesets.BigAssCircle/osu.Game.Rulesets.BigAssCircle.csproj`
- **Visual tests:** run the `osu.Game.Rulesets.BigAssCircle.Tests` project (VisualTestRunner → OsuTestBrowser).
  `TestSceneOsuPlayer` hand-authors a beatmap in code via `CreateBeatmap` — that's the fastest way to try a path.
- **Runtime logs (exceptions, etc.):** `%APPDATA%\osu\logs\*.runtime.log` (the test host name is `osu`).
  Newest by mtime = most recent run. This is where an unhandled gameplay exception ends up.

## Core rendering model (learn this first)

- **Polar coordinate system** centred on the playfield. Convention everywhere (buttons, `Arc`, paths):
  `x = cos(θ)·r`, `y = -sin(θ)·r` (screen y is down). So `θ = 0` points **right** (East) and θ increases
  **counter-clockwise**: East→right, North→up, West→left, South→down. `CardinalDirection.ToRadians()`
  gives East=0, North=π/2, West=π, South=3π/2, matching those directions.
- The **scroll algorithm maps TIME → RADIUS**: an object sits at the centre one `TimeRange` before its
  time and reaches the ring (`ScrollLength = min(width,height)/2`) exactly at its time. That is *why*
  things "emerge from the centre."
- **`BigAssCircleScrollingHitObjectContainer`** owns this mapping and drives positioning. Public API:
  - `ProgressAtTime(time)` — distance clamped to `[0, ScrollLength]`. Used by buttons.
  - `DistanceFromCentreAtTime(time)` — **unclamped** (negative = not yet emerged, `> ScrollLength` =
    already consumed by the ring). Used by paths so they can clip against both boundaries.
  - `ScrollLength` — ring radius.
  - The container is `[Cached]`. Since the playfield split there are **several instances** — one per
    `Lane` (holding that direction's buttons) plus one on the `Ring` (holding cross-lane paths). All are
    full-size, so they compute identical geometry. A `DrawableBacPath` `[Resolved]`s the *nearest* one
    (its parent, the ring's), which is why paths must live in the ring's container, not a lane's. See
    "Playfield structure" below.
- The container's alive-loop only *positions* `DrawableBacButtonHitObject`. **Paths self-manage their
  geometry each frame** in `DrawableBacPath.Update`, so the loop skips non-buttons (don't re-introduce a
  blanket cast — it will crash on paths).

## Hit objects

- `BacHitObject` — abstract base (`Objects/`).
- `BacButtonHitObject` — a single hit in one of four `CardinalDirection`s.
- `BacPathStartHitObject : BacHitObject, IHasDuration` — owns a `BacPath` (`DirectionDeg` = initial
  angle; a list of `BacPathControlPoint`). Nests one `BacPathChildHitObject` per control point.
  `IHasDuration` gives it a correct lifetime (last node reaches the ring at `EndTime`).
- Drawable selection: `DrawableBigAssCircleRuleset.CreateDrawableRepresentation` dispatches on type.

## Playfield structure: ring & lanes (mania-style)

Modelled on mania's `ManiaPlayfield → Stage → Column` to get **lane-independent note-lock**. Three levels:

- **`BigAssCirclePlayfield`** (= `ManiaPlayfield`) — thin top-level router. Nests one `Ring`, forwards
  `Add`/`Remove` to it, and keeps the global overlays that aren't tied to a lane (stick indicators, debug).
- **`Ring`** (= `Stage`) — the circular arena. Owns the shared furniture in correct z-order (radial lines
  behind, outer `Arc` in front), holds cross-lane **paths** in its own container (like a stage holds bar
  lines — this is also the geometry authority paths resolve), nests the four `Lane`s, and routes each
  button to `lanes[(int)Direction]`. **Lanes are created in the constructor, not the BDL**, so a routed
  `Add` never hits a null lane (mirrors `Stage`).
- **`Lane`** (= `Column`) — one per `CardinalDirection`. Owns its own hit-object container (its buttons),
  a `BacOrderedHitPolicy`, and its `PlayfieldKeybeam`. Wires `CheckHittable` onto its buttons in
  `OnNewDrawableHitObject` and force-misses skipped earlier notes in `OnNewResult`.

**Why it's lane-independent:** each lane's hit policy only ever sees *its own* container's `AliveObjects`,
so `GetNext`/note-lock is naturally scoped per direction. Paths are cross-directional, so they are **not**
lane objects — they live in the ring's container.

**Note-lock + input ordering (this was a real bug — read before touching `OnPressed`):**

- `DrawableBacButtonHitObject.OnPressed` **must `return UpdateResult(true)`** — i.e. consume the press when
  it lands a hit. Returning `false` (an earlier iteration did this "so the keybeam shows") lets one tap
  fall through and hit *every* in-window note in the lane, defeating note-lock.
- `HitObjectContainer.Compare` already orders **earlier objects to the front of the input queue**, so the
  earliest hittable note is the one that consumes.
- Because a hit now consumes the press, the **keybeam must observe it first**. `Lane` draws the keybeam at
  the back via `CreateProxy()` but adds the real drawable *in front* of the hit objects for input; it
  returns `false`, then the button consumes. (Mirrors mania's `DefaultKeyArea` sitting in front / proxied
  columns — draw order and input order are deliberately decoupled.)
- `BacOrderedHitPolicy.IsHittable` uses mania's **lenient** formula (`next == null || time <
  next.StartTime`) + consumption + `HandleHit` force-miss — **not** a strict "only the earliest is
  hittable" gate. The strict version would let a pending *missed* note block the next one until it
  auto-misses.

## The path system (most hard-won detail lives here)

A path = **start node** `(angle = DirectionDeg, time = StartTime)` + control points. Control point `i`
defines **node `i+1`** at `(DirectionDeg + RotationOffset, StartTime + TimeOffset)`. It renders as a
polyline of `Box` segments in polar space (`DrawableBacPath.updatePath`).

- **Radius is linear-in-time per link.** Each link is clipped to the visible band `[0, ScrollLength]`
  with Liang–Barsky (`clipToBand`). Inner crossing (r=0) = the **emergence front** creeping out from the
  centre; outer crossing (r=ring) = **consumption** at the edge. The ring-contact point slides
  continuously along the curve — **never clamp radius** (clamping pins/anchors the contact and breaks
  both emergence and the sweep).
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

## Gotchas / patterns (each cost a debugging cycle)

- **Path drawable typing:** `DrawableBacPath : DrawableHitObject<BacHitObject>` (the *base* object type)
  with a shadowed `public new BacPathStartHitObject HitObject => (BacPathStartHitObject)base.HitObject;`
  — mirrors `DrawableSlider`. Generic drawables are **invariant**, so it must be typed to the base to be
  returned from `CreateDrawableRepresentation`.
- **Nested hit objects need a drawable AND a home in the tree.** `CreateNestedHitObject` must return
  non-null for every nested object (else `CreateNestedHitObject returned null …`). *And* the nested
  drawables must be added to a container in the tree (`AddNestedHitObject` → `nestedContainer`), or they
  never receive a clock and crash with an NRE in `OnKilled`/`UpdateResult`.
- **Gameplay-affecting = opt-in.** Anything that changes a path's shape/feel must default to the existing
  exact behaviour and be opt-in per node/path (see `Smooth`/`SweepEasing` defaults). 

## Layout

```
osu.Game.Rulesets.BigAssCircle/
  Objects/            hit objects (BacHitObject, BacButtonHitObject, BacPathStart/Child)
  Objects/Drawables/  drawable representations (DrawableBacButtonHitObject, DrawableBacPath/Child)
  Core/               enums (CardinalDirection, ...) and Core/Path (BacPath, BacPathControlPoint)
  UI/                 BigAssCirclePlayfield → Ring → Lane, scrolling container, Arc (the ring),
                      BacOrderedHitPolicy (note-lock), keybeam, DrawableRuleset
  Beatmaps/           BigAssCircleBeatmapConverter (osu → this ruleset)
osu.Game.Rulesets.BigAssCircle.Tests/   TestSceneOsuPlayer (in-code beatmap), VisualTestRunner
```
