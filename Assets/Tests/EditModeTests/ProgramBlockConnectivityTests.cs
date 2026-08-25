using NUnit.Framework;
using TuringSimulator.Core.ProgramGraph;

namespace EditModeTests
{
    public class ProgramBlockConnectivityTests
    {
        [Test]
        public void Union_MergesIntoSameComponent()
        {
            IProgramBlockConnectivity c = new ProgramBlockConnectivity();
            c.Clear();
            c.Union("a", "b");
            c.Union("b", "c");

            Assert.That(c.SameComponent("a", "c"), Is.True);
            Assert.That(c.SameComponent("a", c.StartNodeId), Is.False);
        }

        [Test]
        public void Union_WithStart_TouchesStartForest()
        {
            IProgramBlockConnectivity c = new ProgramBlockConnectivity();
            c.Clear();
            c.Union(c.StartNodeId, "entry");
            c.Union("entry", "next");

            Assert.That(c.SameComponent("next", c.StartNodeId), Is.True);
            Assert.That(c.SameComponent("orphan", c.StartNodeId), Is.False);
        }

        [Test]
        public void Rebuild_AfterDisconnect_SplitsComponents()
        {
            IProgramBlockConnectivity c = new ProgramBlockConnectivity();
            c.Rebuild(
                new[] { c.StartNodeId, "a", "b", "c" },
                new[] { (c.StartNodeId, "a"), ("a", "b"), ("b", "c") });

            Assert.That(c.SameComponent("c", c.StartNodeId), Is.True);

            c.Rebuild(
                new[] { c.StartNodeId, "a", "b", "c" },
                new[] { (c.StartNodeId, "a"), ("b", "c") });

            Assert.That(c.SameComponent("a", c.StartNodeId), Is.True);
            Assert.That(c.SameComponent("c", c.StartNodeId), Is.False);
            Assert.That(c.SameComponent("b", "c"), Is.True);
        }
    }
}
