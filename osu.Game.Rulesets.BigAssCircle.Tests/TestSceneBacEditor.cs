using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets.BigAssCircle.Core;
using osu.Game.Rulesets.BigAssCircle.Edit;
using osu.Game.Rulesets.BigAssCircle.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.BigAssCircle.Tests
{
    [TestFixture]
    public partial class TestSceneBacEditor : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new BigAssCircleRuleset();

        private BacEditorPlayfield playfield => Editor.ChildrenOfType<BacEditorPlayfield>().Single();

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddUntilStep("wait for composer", () => Editor.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.IsLoaded == true);
        }

        private HashSet<HitObject> objectsBeforePlacement = null!;

        private CardinalNote? placedNote => placedObject<CardinalNote>();

        private T? placedObject<T>() where T : HitObject => EditorBeatmap.HitObjects.Except(objectsBeforePlacement).OfType<T>().SingleOrDefault();

        private void storeExistingObjects() => AddStep("store existing objects", () => objectsBeforePlacement = EditorBeatmap.HitObjects.ToHashSet());

        /// <summary>Screen position of an absolute angle on the grid (origin-independent via the mapping).</summary>
        private Vector2 positionAtAngle(float angleDeg, float yFrac = 0.5f)
        {
            var quad = playfield.ScreenSpaceDrawQuad;
            float xFrac = EditorAngleMapping.ToX(angleDeg);
            return new Vector2(quad.TopLeft.X + quad.Width * xFrac, quad.TopLeft.Y + quad.Height * yFrac);
        }

        /// <summary>The current on-screen position of an object (slightly above its bottom-origin point, inside the sprite).</summary>
        private Vector2 screenPositionOf(BacHitObject hitObject)
        {
            var container = playfield.HitObjectContainer;
            var screen = container.ScreenSpacePositionAtTime(hitObject.StartTime);
            float localX = EditorAngleMapping.ToX(((IHasAngle)hitObject).AngleDeg) * container.DrawWidth;
            screen.X = container.ToScreenSpace(new Vector2(localX, 0)).X;
            return screen - new Vector2(0, 10);
        }

        [Test]
        public void TestPlaceCardinalNote()
        {
            storeExistingObjects();
            AddStep("select note tool", () => InputManager.Key(Key.Number2));
            // slightly off the South line (270°), should snap onto it.
            AddStep("move near south line", () => InputManager.MoveMouseTo(positionAtAngle(264)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("note placed at 270", () => placedNote?.AngleDeg, () => Is.EqualTo(270));
        }

        [Test]
        public void TestSelectAndDragNote()
        {
            storeExistingObjects();
            AddStep("select note tool", () => InputManager.Key(Key.Number2));
            AddStep("move to south line", () => InputManager.MoveMouseTo(positionAtAngle(270)));
            AddStep("click to place", () => InputManager.Click(MouseButton.Left));
            AddAssert("note placed", () => placedNote != null);

            // placement auto-seeks the clock to the new note, which scrolls it to the judgement line.
            AddUntilStep("wait for seek to note", () => Precision.AlmostEquals(EditorClock.CurrentTime, placedNote!.StartTime, 1));

            AddStep("switch to select tool", () => InputManager.Key(Key.Number1));
            AddStep("move to note position", () => InputManager.MoveMouseTo(screenPositionOf(placedNote!)));
            AddStep("click to select", () => InputManager.Click(MouseButton.Left));
            AddAssert("note selected", () => EditorBeatmap.SelectedHitObjects.SingleOrDefault() == placedNote);

            AddStep("start drag", () =>
            {
                InputManager.MoveMouseTo(screenPositionOf(placedNote!));
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("drag one 45° increment right", () => InputManager.MoveMouseTo(
                InputManager.CurrentState.Mouse.Position + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 45f / EditorAngleMapping.TOTAL_DEGREES, 0)));
            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("note rotated to 315", () => placedNote?.AngleDeg, () => Is.EqualTo(315));
        }

        [Test]
        public void TestPlaceHoldNoteWithDrag()
        {
            storeExistingObjects();
            AddStep("select hold tool", () => InputManager.Key(Key.Number3));
            AddStep("move to playfield", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.6f)));
            AddStep("press", () => InputManager.PressButton(MouseButton.Left));
            // downward scrolling: dragging upward extends toward later times.
            AddStep("drag upward", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.3f)));
            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("hold placed with duration", () => placedObject<HoldNote>()?.Duration > 0);
        }

        [Test]
        public void TestPlaceShoulderNotePicksNearerSide()
        {
            storeExistingObjects();
            AddStep("select shoulder tool", () => InputManager.Key(Key.Number4));
            // Left strip is the West–South boundary (225°); Right strip the East–North boundary (45°).
            AddStep("move near left strip", () => InputManager.MoveMouseTo(positionAtAngle(225)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("left shoulder placed", () => placedObject<ShoulderNote>()?.Side == HorizontalDirection.Left);

            storeExistingObjects();
            AddStep("move near right strip", () => InputManager.MoveMouseTo(positionAtAngle(45, 0.4f)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("right shoulder placed", () => placedObject<ShoulderNote>()?.Side == HorizontalDirection.Right);
        }

        [Test]
        public void TestPlaceSlams()
        {
            storeExistingObjects();
            AddStep("select center slam tool", () => InputManager.Key(Key.Number5));
            AddStep("move to playfield", () => InputManager.MoveMouseTo(positionAtAngle(270)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("center slam placed at 270", () => placedObject<BacSlamCentered>()?.AngleDeg, () => Is.EqualTo(270));

            storeExistingObjects();
            AddStep("select edge slam tool", () => InputManager.Key(Key.Number6));
            AddStep("move to playfield", () => InputManager.MoveMouseTo(positionAtAngle(315, 0.4f)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("edge slam placed at 315", () => placedObject<BacSlamEdge>()?.AngleDeg, () => Is.EqualTo(315));
        }

        [Test]
        public void TestPlaceSliderMultiClick()
        {
            storeExistingObjects();
            AddStep("select slider tool", () => InputManager.Key(Key.Number7));
            AddStep("move to body start", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.7f)));
            AddStep("click body", () => InputManager.Click(MouseButton.Left));
            AddStep("move to first node", () => InputManager.MoveMouseTo(positionAtAngle(315, 0.5f)));
            AddStep("click node 1", () => InputManager.Click(MouseButton.Left));
            AddStep("move to second node", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.3f)));
            AddStep("click node 2", () => InputManager.Click(MouseButton.Left));
            AddStep("right click to commit", () => InputManager.Click(MouseButton.Right));

            AddAssert("slider placed", () => placedObject<SliderBody>() != null);
            AddAssert("slider at 270", () => placedObject<SliderBody>()!.AngleDeg, () => Is.EqualTo(270));
            AddAssert("two nodes", () => placedObject<SliderBody>()!.Path.ControlPoints.Count, () => Is.EqualTo(2));
            AddAssert("node offsets are +45 then 0", () =>
            {
                var cps = placedObject<SliderBody>()!.Path.ControlPoints;
                return cps[0].RotationOffset == 45 && cps[1].RotationOffset == 0;
            });
            AddAssert("node times ascend", () =>
            {
                var cps = placedObject<SliderBody>()!.Path.ControlPoints;
                return cps[0].TimeOffset > 0 && cps[1].TimeOffset > cps[0].TimeOffset;
            });
        }

        [Test]
        public void TestInsertSliderNodeWithHotkey()
        {
            storeExistingObjects();
            AddStep("select slider tool", () => InputManager.Key(Key.Number7));
            AddStep("move to body start", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.7f)));
            AddStep("click body", () => InputManager.Click(MouseButton.Left));
            AddStep("move to node", () => InputManager.MoveMouseTo(positionAtAngle(270, 0.3f)));
            AddStep("click node", () => InputManager.Click(MouseButton.Left));
            AddStep("right click to commit", () => InputManager.Click(MouseButton.Right));
            AddAssert("slider placed", () => placedObject<SliderBody>() != null);

            AddUntilStep("wait for seek to slider", () => Precision.AlmostEquals(EditorClock.CurrentTime, placedObject<SliderBody>()!.StartTime, 1));

            AddStep("switch to select tool", () => InputManager.Key(Key.Number1));
            AddStep("move to slider head", () => InputManager.MoveMouseTo(screenPositionOf(placedObject<SliderBody>()!)));
            AddStep("click to select", () => InputManager.Click(MouseButton.Left));
            AddAssert("slider selected", () => EditorBeatmap.SelectedHitObjects.SingleOrDefault() == placedObject<SliderBody>());

            AddStep("move cursor to mid-duration", () =>
            {
                var slider = placedObject<SliderBody>()!;
                var container = playfield.HitObjectContainer;
                var screen = container.ScreenSpacePositionAtTime(slider.StartTime + slider.Duration / 2);
                screen.X = positionAtAngle(315).X;
                InputManager.MoveMouseTo(screen);
            });
            AddStep("press T", () => InputManager.Key(Key.T));
            AddAssert("node inserted", () => placedObject<SliderBody>()!.Path.ControlPoints.Count, () => Is.EqualTo(2));
            AddAssert("nodes remain time-ordered", () =>
            {
                var cps = placedObject<SliderBody>()!.Path.ControlPoints;
                return cps[0].TimeOffset < cps[1].TimeOffset;
            });
        }

        [Test]
        public void TestSliderCrossingWrapSeamRendersWrapCopies()
        {
            storeExistingObjects();
            AddStep("select slider tool", () => InputManager.Key(Key.Number7));
            // body one snap step left of the seam (135°); the node one step past it on the other side.
            // MinimalDiff must take the short way across the seam (+90), pushing the path off the grid's
            // right edge — which the polyline must re-enter from the left as a second wrap copy.
            AddStep("move to body start", () => InputManager.MoveMouseTo(positionAtAngle(90, 0.7f)));
            AddStep("click body", () => InputManager.Click(MouseButton.Left));
            AddStep("move node across seam", () => InputManager.MoveMouseTo(positionAtAngle(180, 0.4f)));
            AddStep("click node", () => InputManager.Click(MouseButton.Left));
            AddStep("right click to commit", () => InputManager.Click(MouseButton.Right));

            AddAssert("slider placed", () => placedObject<SliderBody>() != null);
            AddAssert("offset takes the short way (+90)", () => placedObject<SliderBody>()!.Path.ControlPoints.Single().RotationOffset, () => Is.EqualTo(90));

            AddUntilStep("polyline renders two wrap copies", () =>
                Editor.ChildrenOfType<Edit.Drawables.EditorDrawableSliderBody>()
                      .SingleOrDefault(d => d.HitObject == placedObject<SliderBody>())
                      ?.ChildrenOfType<osu.Framework.Graphics.Lines.SmoothPath>().Count() == 2);
        }

        [Test]
        public void TestSelectViaGhostTwin()
        {
            storeExistingObjects();
            AddStep("select note tool", () => InputManager.Key(Key.Number2));
            // 150° is within 30° of the left edge (135°), so once snapped it shows a twin in the right band.
            AddStep("move near left edge", () => InputManager.MoveMouseTo(positionAtAngle(150)));
            AddStep("click to place", () => InputManager.Click(MouseButton.Left));
            AddAssert("note placed", () => placedNote != null);

            AddUntilStep("wait for seek to note", () => Precision.AlmostEquals(EditorClock.CurrentTime, placedNote!.StartTime, 1));

            AddStep("switch to select tool", () => InputManager.Key(Key.Number1));
            AddStep("move to ghost twin in right band", () =>
            {
                var main = screenPositionOf(placedNote!);
                float wrapOffset = playfield.ScreenSpaceDrawQuad.Width * 360f / EditorAngleMapping.TOTAL_DEGREES;
                InputManager.MoveMouseTo(main + new Vector2(wrapOffset, 0));
            });
            AddStep("click twin", () => InputManager.Click(MouseButton.Left));
            AddAssert("note selected via twin", () => EditorBeatmap.SelectedHitObjects.SingleOrDefault() == placedNote);
        }

        [Test]
        public void TestPlacementInGhostBandWrapsAngle()
        {
            storeExistingObjects();
            AddStep("select note tool", () => InputManager.Key(Key.Number2));
            // 15° into the RIGHT band, where the unwrapped angle runs past 360° — the snap lands on the
            // unwrapped 495°, which wraps (mod 360) to 135°, the seam angle.
            Vector2 rightBandPosition()
            {
                var quad = playfield.ScreenSpaceDrawQuad;
                float xFrac = (EditorAngleMapping.GHOST_DEGREES + 360f + 15f) / EditorAngleMapping.TOTAL_DEGREES;
                return new Vector2(quad.TopLeft.X + quad.Width * xFrac, quad.Centre.Y);
            }

            AddStep("move into right ghost band", () => InputManager.MoveMouseTo(rightBandPosition()));
            AddAssert("composer wraps band position to 135", () =>
            {
                var composer = Editor.ChildrenOfType<BigAssCircleHitObjectComposer>().Single();
                var result = composer.FindSnappedAngleTimeAndPosition(rightBandPosition());
                return result is BacSnapResult bac && bac.AngleDeg == 135;
            });
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("note placed", () => placedNote != null);
            AddAssert("angle wrapped to 135", () => placedNote!.AngleDeg, () => NUnit.Framework.Is.EqualTo(135));
        }
    }
}
