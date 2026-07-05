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
  `x = sin(θ)·r`, `y = cos(θ)·r`.
- The **scroll algorithm maps TIME → RADIUS**: an object sits at the centre one `TimeRange` before its
  time and reaches the ring (`ScrollLength = min(width,height)/2`) exactly at its time. That is *why*
  things "emerge from the centre."
- **`BigAssCircleScrollingHitObjectContainer`** owns this mapping and drives positioning. Public API:
  - `ProgressAtTime(time)` — distance clamped to `[0, ScrollLength]`. Used by buttons.
  - `DistanceFromCentreAtTime(time)` — **unclamped** (negative = not yet emerged, `> ScrollLength` =
    already consumed by the ring). Used by paths so they can clip against both boundaries.
  - `ScrollLength` — ring radius.
  - The container is `[Cached]`; drawables resolve it via `[Resolved]`.
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
  UI/                 playfield, scrolling container, Arc (the ring), DrawableRuleset
  Beatmaps/           BigAssCircleBeatmapConverter (osu → this ruleset)
osu.Game.Rulesets.BigAssCircle.Tests/   TestSceneOsuPlayer (in-code beatmap), VisualTestRunner
```
