using System.Collections.Generic;
using NUnit.Framework;
using TuringSimulator.Core.ProgramGraph;

namespace EditModeTests
{
    public class ExecutionWireHighlightTests
    {
        [Test]
        public void TryGetTransitionBlocks_KnownStates_ReturnsBlockIds()
        {
            IReadOnlyDictionary<int, string> map = new Dictionary<int, string>
            {
                { 0, "write" },
                { 1, "move" },
            };

            var ok = ExecutionWireHighlight.TryGetTransitionBlocks(map, 0, 1, out var from, out var to);

            Assert.That(ok, Is.True);
            Assert.That(from, Is.EqualTo("write"));
            Assert.That(to, Is.EqualTo("move"));
        }

        [Test]
        public void TryGetTransitionBlocks_UnknownNextState_Fails()
        {
            IReadOnlyDictionary<int, string> map = new Dictionary<int, string>
            {
                { 0, "write" },
            };

            var ok = ExecutionWireHighlight.TryGetTransitionBlocks(map, 0, 99, out var from, out var to);

            Assert.That(ok, Is.False);
            Assert.That(from, Is.EqualTo("write"));
            Assert.That(to, Is.Null);
        }

        [Test]
        public void TryGetTransitionBlocks_NullMap_Fails()
        {
            var ok = ExecutionWireHighlight.TryGetTransitionBlocks(null, 0, 1, out _, out _);
            Assert.That(ok, Is.False);
        }
    }
}
