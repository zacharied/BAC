using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Rulesets.BigAssCircle.Edit.Drawables;
using osu.Game.Rulesets.Edit;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.BigAssCircle.Tests
{
    /// <summary>
    /// Guards against sprite auto-sizing regressions: assigning <see cref="Sprite.Texture"/> before
    /// <see cref="osu.Framework.Graphics.Drawable.RelativeSizeAxes"/> makes the raw texture size act as
    /// a relative multiplier, blowing a 36px note up to thousands of pixels.
    /// </summary>
    [TestFixture]
    public partial class TestSceneBacEditorDrawableSizes : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new BigAssCircleRuleset();

        [Test]
        public void TestEditorSpritesStayWithinTheirPieces()
        {
            AddUntilStep("wait for composer", () => Editor.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.IsLoaded == true);
            AddUntilStep("has cardinal drawables", () => Editor.ChildrenOfType<EditorDrawableCardinalNote>().Any());
            AddAssert("all editor sprites fit their pieces", () =>
                Editor.ChildrenOfType<EditorSpritePiece>()
                      .SelectMany(p => p.ChildrenOfType<Sprite>().Select(s => (piece: p, sprite: s)))
                      .All(x => x.sprite.ScreenSpaceDrawQuad.Width <= x.piece.ScreenSpaceDrawQuad.Width + 1));
        }
    }
}
