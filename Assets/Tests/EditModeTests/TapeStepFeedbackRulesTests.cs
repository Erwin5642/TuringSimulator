using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine.Tape;

namespace EditModeTests
{
    public class TapeStepFeedbackRulesTests
    {
        [Test]
        public void IsReadMatch_SameSymbols_IsTrue()
        {
            Assert.That(TapeStepFeedbackRules.IsReadMatch(Symbol.Gear, Symbol.Gear), Is.True);
            Assert.That(TapeStepFeedbackRules.IsReadMatch(Symbol.Blank, Symbol.Blank), Is.True);
        }

        [Test]
        public void IsReadMatch_DifferentSymbols_IsFalse()
        {
            Assert.That(TapeStepFeedbackRules.IsReadMatch(Symbol.Gear, Symbol.Nut), Is.False);
            Assert.That(TapeStepFeedbackRules.IsReadMatch(Symbol.Gear, Symbol.Blank), Is.False);
        }

        [Test]
        public void ResolveWriteEffect_Unchanged_IsNone()
        {
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Gear, Symbol.Gear),
                Is.EqualTo(TapeWriteEffectKind.None));
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Blank, Symbol.Blank),
                Is.EqualTo(TapeWriteEffectKind.None));
        }

        [Test]
        public void ResolveWriteEffect_PlaceOrReplaceMaterial_IsWrite()
        {
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Blank, Symbol.Gear),
                Is.EqualTo(TapeWriteEffectKind.Write));
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Gear, Symbol.Nut),
                Is.EqualTo(TapeWriteEffectKind.Write));
        }

        [Test]
        public void ResolveWriteEffect_ClearMaterial_IsDelete()
        {
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Gear, Symbol.Blank),
                Is.EqualTo(TapeWriteEffectKind.Delete));
            Assert.That(
                TapeStepFeedbackRules.ResolveWriteEffect(Symbol.Nut, Symbol.None),
                Is.EqualTo(TapeWriteEffectKind.Delete));
        }
    }
}
