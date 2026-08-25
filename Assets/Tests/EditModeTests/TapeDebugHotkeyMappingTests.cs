using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;

namespace EditModeTests
{
    public class TapeDebugHotkeyMappingTests
    {
        [Test]
        public void Reduce_IdleLeftRight_MovesTape()
        {
            var left = TapeDebugHotkeyMapping.Reduce(false, TapeDebugKey.MoveLeft);
            Assert.That(left.AwaitingWrite, Is.False);
            Assert.That(left.Move, Is.EqualTo(MoveDirection.Left));
            Assert.That(left.Write, Is.Null);

            var right = TapeDebugHotkeyMapping.Reduce(false, TapeDebugKey.MoveRight);
            Assert.That(right.Move, Is.EqualTo(MoveDirection.Right));
        }

        [Test]
        public void Reduce_W_ArmsWrite_ThenDigitWritesSymbol()
        {
            var armed = TapeDebugHotkeyMapping.Reduce(false, TapeDebugKey.ArmWrite);
            Assert.That(armed.AwaitingWrite, Is.True);
            Assert.That(armed.Move, Is.Null);
            Assert.That(armed.Write, Is.Null);

            AssertSymbol(TapeDebugKey.WriteBlank, Symbol.Blank);
            AssertSymbol(TapeDebugKey.WriteGear, Symbol.Gear);
            AssertSymbol(TapeDebugKey.WriteNut, Symbol.Nut);
            AssertSymbol(TapeDebugKey.WriteScrew, Symbol.Screw);
        }

        [Test]
        public void Reduce_AwaitingWrite_IgnoresArrows()
        {
            var outcome = TapeDebugHotkeyMapping.Reduce(true, TapeDebugKey.MoveLeft);
            Assert.That(outcome.AwaitingWrite, Is.True);
            Assert.That(outcome.Move, Is.Null);
            Assert.That(outcome.Write, Is.Null);
        }

        [Test]
        public void Reduce_IdleDigit_DoesNotWrite()
        {
            var outcome = TapeDebugHotkeyMapping.Reduce(false, TapeDebugKey.WriteGear);
            Assert.That(outcome.AwaitingWrite, Is.False);
            Assert.That(outcome.Write, Is.Null);
        }

        [Test]
        public void Reduce_AwaitingWrite_WOrCancel_Disarms()
        {
            var cancel = TapeDebugHotkeyMapping.Reduce(true, TapeDebugKey.Cancel);
            Assert.That(cancel.AwaitingWrite, Is.False);
            Assert.That(cancel.Write, Is.Null);

            var rearm = TapeDebugHotkeyMapping.Reduce(true, TapeDebugKey.ArmWrite);
            Assert.That(rearm.AwaitingWrite, Is.False);
        }

        private static void AssertSymbol(TapeDebugKey key, Symbol expected)
        {
            var outcome = TapeDebugHotkeyMapping.Reduce(true, key);
            Assert.That(outcome.AwaitingWrite, Is.False);
            Assert.That(outcome.Write, Is.EqualTo(expected));
        }
    }
}
