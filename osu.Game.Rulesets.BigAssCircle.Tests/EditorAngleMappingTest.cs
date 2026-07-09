using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.BigAssCircle.Edit;

namespace osu.Game.Rulesets.BigAssCircle.Tests
{
    [TestFixture]
    public class EditorAngleMappingTest
    {
        [Test]
        public void TestWrapCopiesFullyOnGrid()
        {
            Assert.That(EditorAngleMapping.VisibleWrapCopies(50, 100), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void TestWrapCopiesPointRange()
        {
            Assert.That(EditorAngleMapping.VisibleWrapCopies(180, 180), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void TestWrapCopiesCrossingRightEdge()
        {
            // sweeps past 360: the overhang re-enters from the left as copy k = 1.
            Assert.That(EditorAngleMapping.VisibleWrapCopies(350, 380), Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void TestWrapCopiesCrossingLeftEdge()
        {
            // negative sweep below 0: the overhang re-enters from the right as copy k = −1.
            Assert.That(EditorAngleMapping.VisibleWrapCopies(-20, 10), Is.EqualTo(new[] { -1, 0 }));
        }

        [Test]
        public void TestWrapCopiesMultiTurn()
        {
            // two-turn sweep starting at the grid's left edge: one copy per turn (k = 1, 2), plus the
            // head itself re-shown in the right ghost band (k = −1), plus the unshifted copy.
            Assert.That(EditorAngleMapping.VisibleWrapCopies(0, 765), Is.EqualTo(new[] { -1, 0, 1, 2 }));
        }

        [Test]
        public void TestWrapCopiesNeverEmpty()
        {
            // any range overlapping the window must at least contain its own copy.
            for (int start = 0; start < 360; start += 15)
                Assert.That(EditorAngleMapping.VisibleWrapCopies(start, start + 10).ToList(), Does.Contain(0), $"range starting at {start}");
        }
    }
}
